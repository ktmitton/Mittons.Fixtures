using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Containers.Gateways.Docker;
using Mittons.Fixtures.Core.Attributes;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration.Containers.Gateways;

public class ContainerGatewayTests
{
    public class ImageTests : IClassFixture<DockerCleanupFixture>
    {
        private readonly DockerCleanupFixture _dockerCleanupFixture;

        public ImageTests(DockerCleanupFixture dockerCleanupFixture)
        {
            _dockerCleanupFixture = dockerCleanupFixture;
        }

        [Theory]
        [InlineData("alpine:3.14")]
        [InlineData("alpine:3.15")]
        public async Task CreateContainerAsync_WhenAnImageNameIsProvided_ExpectTheStartedContainerToUseTheImage(string expectedImageName)
        {
            // Arrange
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();
            var command = string.Empty;

            // Act
            var containerId = await gateway.CreateContainerAsync(expectedImageName, PullOption.Missing, labels, command, default, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddContainer(containerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"ps -a --filter id={containerId} --format \"{{{{.Image}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualImageName = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

                Assert.Equal(expectedImageName, actualImageName);
            }
        }
    }

    public class LabelsTests : IClassFixture<DockerCleanupFixture>
    {
        private readonly DockerCleanupFixture _dockerCleanupFixture;

        public LabelsTests(DockerCleanupFixture dockerCleanupFixture)
        {
            _dockerCleanupFixture = dockerCleanupFixture;
        }

        [Fact]
        public async Task CreateContainerAsync_WhenLabelsAreProvided_ExpectTheStartedContainerToHaveTheLabelsApplied()
        {
            // Arrange
            var imageName = "alpine:3.15";
            var expectedLabels = new Dictionary<string, string>
            {
                { "mylabel", "myvalue" },
                { "myotherlabel", "myothervalue" }
            };
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();
            var command = string.Empty;

            // Act
            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, expectedLabels, command, default, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddContainer(containerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{json .Config.Labels}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualLabels = JsonSerializer.Deserialize<Dictionary<string, string>>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false)) ?? new Dictionary<string, string>();

                Assert.All(expectedLabels, x => Assert.Equal(x.Value, actualLabels[x.Key]));
            }
        }
    }

    public class LifetimeTests : IClassFixture<DockerCleanupFixture>
    {
        private readonly DockerCleanupFixture _dockerCleanupFixture;

        private record Volume(string Name);

        public LifetimeTests(DockerCleanupFixture dockerCleanupFixture)
        {
            _dockerCleanupFixture = dockerCleanupFixture;
        }

        [Theory]
        [InlineData(PullOption.Always)]
        [InlineData(PullOption.Missing)]
        [InlineData(PullOption.Never)]
        public async Task CreateContainerAsync_WhenTheContainerIsCreated_ExpectThePullOptionToBeSet(PullOption pullOption)
        {
            // Arrange
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var processDebugger = new ProcessDebugger();
            var gateway = new ContainerGateway(processDebugger);
            var command = string.Empty;

            // Act
            var containerId = await gateway.CreateContainerAsync("alpine:3.15", pullOption, labels, command, default, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddContainer(containerId);

            // Assert
            var log = processDebugger.CallLog.Pop();

            Assert.Contains($"--pull {pullOption.ToString().ToLower()}", log.Arguments);
        }

        [Fact]
        public async Task RemoveContainerAsync_WhenTheContainerHasUnnamedVolumes_ExpectTheVolumesToBeRemoved()
        {
            // Arrange
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            var containerId = string.Empty;

            var volumeOptions = new string[] { "--volume /var", "--volume random" };

            var extensionFilename = Path.GetTempFileName();
            var noExtensionFilename = Path.GetTempFileName();

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"run -d --volume /var --volume random redis:alpine";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                containerId = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false) ?? string.Empty;
            }

            _dockerCleanupFixture.AddContainer(containerId);

            var volumeNames = Enumerable.Empty<string>();

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{json .Mounts}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                volumeNames = JsonSerializer.Deserialize<IEnumerable<Volume>>(output)?.Select(x => x.Name) ?? Enumerable.Empty<string>();
            }

            // Act
            await gateway.RemoveContainerAsync(containerId, cancellationToken).ConfigureAwait(false);

            // Assert
            foreach (var name in volumeNames)
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "docker";
                    process.StartInfo.Arguments = $"volume ls -q --filter Name={name}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;

                    process.Start();
                    await process.WaitForExitAsync().ConfigureAwait(false);

                    var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                    Assert.Empty(output);
                }
            }
        }

        [Fact]
        public async Task RemoveContainerAsync_WhenAnImageNameIsProvided_ExpectTheStartedContainerToUseTheImage()
        {
            // Arrange
            var imageName = "alpine:3.15";
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();
            var command = string.Empty;

            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, command, default, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddContainer(containerId);

            // Act
            await gateway.RemoveContainerAsync(containerId, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"ps -a --filter id={containerId} --format \"{{{{.ID}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Empty(output);
            }
        }
    }

    public class PortTests : IClassFixture<DockerCleanupFixture>
    {
        private readonly DockerCleanupFixture _dockerCleanupFixture;

        public PortTests(DockerCleanupFixture dockerCleanupFixture)
        {
            _dockerCleanupFixture = dockerCleanupFixture;
        }

        [Fact]
        public async Task CreateContainerAsync_WhenAnImageNameHasAnExposedPort_ExpectThePortToBePublished()
        {
            // Arrange
            var imageName = "redis:alpine";
            var port = 6379;
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();
            var command = string.Empty;

            // Act
            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, command, default, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddContainer(containerId);

            // Assert
            using (var portProcess = new Process())
            {
                portProcess.StartInfo.FileName = "docker";
                portProcess.StartInfo.Arguments = $"port {containerId} {port}";
                portProcess.StartInfo.UseShellExecute = false;
                portProcess.StartInfo.RedirectStandardOutput = true;

                portProcess.Start();
                await portProcess.WaitForExitAsync().ConfigureAwait(false);

                var portDetails = (await portProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false) ?? string.Empty);

                Assert.Matches(@"^.*:\d+$", portDetails);
            }
        }
    }

    public class ResourceTests : IClassFixture<DockerCleanupFixture>
    {
        private readonly DockerCleanupFixture _dockerCleanupFixture;

        public ResourceTests(DockerCleanupFixture dockerCleanupFixture)
        {
            _dockerCleanupFixture = dockerCleanupFixture;
        }

        [Theory]
        [InlineData("/var/")]
        [InlineData("/random/")]
        [InlineData("/data/")]
        [InlineData("/tmp/extension.txt")]
        [InlineData("/tmp/noextension")]
        public async Task GetAvailableResourcesAsync_WhenAnImageHasVolumes_ExpectTheVolumesToBeAddedAsResources(string resourcePath)
        {
            // Arrange
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            var containerId = string.Empty;

            var volumeOptions = new string[] { "--volume /var", "--volume random" };

            var extensionFilename = Path.GetTempFileName();
            var noExtensionFilename = Path.GetTempFileName();

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"run -d -p 6379/tcp --volume /var --volume random --volume \"{extensionFilename}:/tmp/extension.txt\" --volume \"{noExtensionFilename}:/tmp/noextension\"  redis:alpine";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                containerId = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false) ?? string.Empty;
            }

            _dockerCleanupFixture.AddContainer(containerId);

            var expectedGuestUri = new Uri($"file://{resourcePath}");
            var expectedHostUri = new Uri($"file://container.{containerId}{resourcePath}");

            // Act
            var resources = await gateway.GetAvailableResourcesAsync(containerId, cancellationToken).ConfigureAwait(false);

            // Assert
            Assert.Contains(resources, x => x.GuestUri == expectedGuestUri && x.HostUri == expectedHostUri);
        }

        [Theory]
        [InlineData("tcp", 6379)]
        [InlineData("tcp", 80)]
        [InlineData("tcp", 443)]
        [InlineData("tcp", 22)]
        [InlineData("udp", 2834)]
        public async Task GetAvailableResourcesAsync_WhenAContainerHasForwardedPorts_ExpectThePortsToBeAddedAsAResource(string scheme, int guestPort)
        {
            // Arrange
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            var expectedGuestUriBuilder = new UriBuilder();
            expectedGuestUriBuilder.Scheme = scheme;
            expectedGuestUriBuilder.Host = "localhost";
            expectedGuestUriBuilder.Port = guestPort;

            var expectedHostUriBuilder = new UriBuilder();
            expectedHostUriBuilder.Scheme = scheme;

            var containerId = string.Empty;

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"run -d --health-cmd \"redis-cli ping\" --health-interval \"1s\" --health-timeout \"1s\" --health-start-period \"1s\" --health-retries 3 -p 6379/tcp -p 80/tcp -p 443/tcp -p 22/tcp -p 2834/udp redis:alpine";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                containerId = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false) ?? string.Empty;
            }

            _dockerCleanupFixture.AddContainer(containerId);

            // Sometimes testing goes to quickly, and the ports are not bound yet, so we call this function to make _sure_ the container is fully up
            await gateway.EnsureContainerIsHealthyAsync(containerId, cancellationToken).ConfigureAwait(false);

            // Act
            var resources = await gateway.GetAvailableResourcesAsync(containerId, cancellationToken).ConfigureAwait(false);

            // Assert
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                expectedHostUriBuilder.Host = "localhost";

                using (var portProcess = new Process())
                {
                    portProcess.StartInfo.FileName = "docker";
                    portProcess.StartInfo.Arguments = $"port {containerId} {guestPort}/{scheme}";
                    portProcess.StartInfo.UseShellExecute = false;
                    portProcess.StartInfo.RedirectStandardOutput = true;

                    portProcess.Start();
                    await portProcess.WaitForExitAsync().ConfigureAwait(false);

                    var portDetails = (await portProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false) ?? string.Empty).Split(":");

                    Assert.Equal(2, portDetails.Length);

                    expectedHostUriBuilder.Port = int.Parse(portDetails[1]);
                }
            }
            else
            {
                using (var ipProcess = new Process())
                {
                    ipProcess.StartInfo.FileName = "docker";
                    ipProcess.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{.NetworkSettings.IPAddress}}}}\"";
                    ipProcess.StartInfo.UseShellExecute = false;
                    ipProcess.StartInfo.RedirectStandardOutput = true;

                    ipProcess.Start();
                    await ipProcess.WaitForExitAsync().ConfigureAwait(false);

                    expectedHostUriBuilder.Host = await ipProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                }

                expectedHostUriBuilder.Port = guestPort;
            }

            Assert.Contains(resources, x => x.GuestUri == expectedGuestUriBuilder.Uri && x.HostUri == expectedHostUriBuilder.Uri);
        }

        [Fact]
        public async Task GetAvailableResourcesAsync_WhenAContainerHasAnExposedButNotForwardedPorts_ExpectThePortToNotBeAddedAsAResource()
        {
            // Arrange
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            var containerId = string.Empty;

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"run -d redis:alpine";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                containerId = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false) ?? string.Empty;
            }

            _dockerCleanupFixture.AddContainer(containerId);

            // Act
            var resources = await gateway.GetAvailableResourcesAsync(containerId, cancellationToken).ConfigureAwait(false);

            // Assert
            Assert.DoesNotContain(resources, x => x.GuestUri.Port == 6379);
        }
    }

    public class CommandTests : IClassFixture<DockerCleanupFixture>
    {
        private readonly DockerCleanupFixture _dockerCleanupFixture;

        public CommandTests(DockerCleanupFixture dockerCleanupFixture)
        {
            _dockerCleanupFixture = dockerCleanupFixture;
        }

        [Theory]
        [InlineData("echo hello")]
        [InlineData("ls")]
        public async Task CreateContainerAsync_WhenCalledWithACommand_ExpectTheContainerToBeStartedWithTheCommand(string expectedCommand)
        {
            // Arrange
            var imageName = "alpine:3.15";
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            // Act
            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, expectedCommand, default, cancellationToken).ConfigureAwait(false);
            _dockerCleanupFixture.AddContainer(containerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{json .Config.Cmd}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = JsonSerializer.Deserialize<string[]>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false)) ?? new string[0];

                var actualCommand = string.Join(" ", output);

                Assert.Equal(expectedCommand, actualCommand);
            }
        }
    }

    public class HealthCheckTests : IClassFixture<DockerCleanupFixture>
    {
        private readonly DockerCleanupFixture _dockerCleanupFixture;

        public HealthCheckTests(DockerCleanupFixture dockerCleanupFixture)
        {
            _dockerCleanupFixture = dockerCleanupFixture;
        }

        private record HealthCheckDescription(bool Disabled, string Command, byte Interval, byte Timeout, byte StartPeriod, byte Retries) : IHealthCheckDescription;

        private record HealthCheckReport(string[] Test, long Interval, long Timeout, long StartPeriod, byte Retries);

        [Fact]
        public async Task CreateContainerAsync_WhenCalledWithADisabledHealthCheck_ExpectTheContainerToHaveNoHealthCheck()
        {
            // Arrange
            var imageName = "alpine:3.15";
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            // Act
            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, default(string), new HealthCheckDescription(true, "test", 1, 1, 1, 1), cancellationToken).ConfigureAwait(false);
            _dockerCleanupFixture.AddContainer(containerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{json .Config.Healthcheck}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualHealthCheck = JsonSerializer.Deserialize<HealthCheckReport>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

                Assert.Equal("NONE", string.Join(" ", actualHealthCheck?.Test ?? new string[0]));
            }
        }

        [Theory]
        [InlineData("echo hello", 1, 1, 1, 1)]
        [InlineData("ls", 2, 3, 4, 5)]
        public async Task CreateContainerAsync_WhenCalledWithAnEnabledHealthCheck_ExpectTheContainerToUseTheHealthCheck(string expectedCommand, byte expectedInterval, byte expectedTimeout, byte expectedStartPeriod, byte expectedRetries)
        {
            // Arrange
            var imageName = "alpine:3.15";
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            long nanosecondModifier = 1000000000;

            var healthCheck = new HealthCheckDescription(false, expectedCommand, expectedInterval, expectedTimeout, expectedStartPeriod, expectedRetries);

            // Act
            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, default(string), healthCheck, cancellationToken).ConfigureAwait(false);
            _dockerCleanupFixture.AddContainer(containerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{json .Config.Healthcheck}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualHealthCheck = JsonSerializer.Deserialize<HealthCheckReport>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

                Assert.Equal($"CMD-SHELL {expectedCommand}", string.Join(" ", actualHealthCheck?.Test ?? new string[0]));
                Assert.Equal(expectedInterval * nanosecondModifier, actualHealthCheck?.Interval);
                Assert.Equal(expectedTimeout * nanosecondModifier, actualHealthCheck?.Timeout);
                Assert.Equal(expectedStartPeriod * nanosecondModifier, actualHealthCheck?.StartPeriod);
                Assert.Equal(expectedRetries, actualHealthCheck?.Retries);
            }
        }

        [Fact]
        public async Task CreateContainerAsync_WhenCalledWithNoHealthCheck_ExpectTheContainerToHaveNoHealthCheck()
        {
            // Arrange
            var imageName = "alpine:3.15";
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            // Act
            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, default(string), default(IHealthCheckDescription), cancellationToken).ConfigureAwait(false);
            _dockerCleanupFixture.AddContainer(containerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{json .Config.Healthcheck}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualHealthCheck = JsonSerializer.Deserialize<HealthCheckReport>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

                Assert.Null(actualHealthCheck);
            }
        }

        [Fact]
        public async Task EnsureContainerIsHealthyAsync_WhenCancellationTokenIsCancelledBeforeHealthCheckPasses_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var imageName = "alpine:3.15";
            var labels = new Dictionary<string, string>();
            var gateway = new ContainerGateway();

            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, default(string), default(IHealthCheckDescription), CancellationToken.None).ConfigureAwait(false);
            _dockerCleanupFixture.AddContainer(containerId);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var cancellationToken = cancellationTokenSource.Token;

            // Act
            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => gateway.EnsureContainerIsHealthyAsync(containerId, cancellationToken));
        }

        [Fact]
        public async Task EnsureContainerIsHealthyAsync_WhenNoHealthCheckIsSetAndContainerIsNotRunningAfterDefaultTime_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var imageName = "alpine:3.15";
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, default(string), default(IHealthCheckDescription), CancellationToken.None).ConfigureAwait(false);
            _dockerCleanupFixture.AddContainer(containerId);

            // Act
            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => gateway.EnsureContainerIsHealthyAsync(containerId, cancellationToken));
        }

        [Fact]
        public async Task EnsureContainerIsHealthyAsync_WhenNoHealthCheckIsSetAndContainerIsRunningForDefaultTime_ExpectCheckToCompleteSuccessfully()
        {
            // Arrange
            var imageName = "redis:alpine";
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, default(string), default(IHealthCheckDescription), CancellationToken.None).ConfigureAwait(false);
            _dockerCleanupFixture.AddContainer(containerId);

            // Act
            // Assert
            await gateway.EnsureContainerIsHealthyAsync(containerId, cancellationToken);
        }

        [Fact]
        public async Task EnsureContainerIsHealthyAsync_WhenAHealthCheckIsSetAndTheContainerDoesNotBecomeHealthyBeforeTheHealthCheckLimit_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var imageName = "redis:alpine";
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            var healthCheck = new HealthCheckDescription(false, "exit 1", 1, 1, 1, 1);

            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, default(string), healthCheck, CancellationToken.None).ConfigureAwait(false);
            _dockerCleanupFixture.AddContainer(containerId);

            // Act
            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => gateway.EnsureContainerIsHealthyAsync(containerId, cancellationToken));
        }

        [Fact]
        public async Task EnsureContainerIsHealthyAsync_WhenAHealthCheckIsSetAndTheContaineBecomesHealthyBeforeTheHealthCheckLimit_ExpectCheckToCompleteSuccessfully()
        {
            // Arrange
            var imageName = "redis:alpine";
            var labels = new Dictionary<string, string>();
            var cancellationToken = new CancellationTokenSource().Token;
            var gateway = new ContainerGateway();

            var healthCheck = new HealthCheckDescription(false, "exit 0", 1, 1, 1, 1);

            var containerId = await gateway.CreateContainerAsync(imageName, PullOption.Missing, labels, default(string), healthCheck, CancellationToken.None).ConfigureAwait(false);
            _dockerCleanupFixture.AddContainer(containerId);

            // Act
            // Assert
            await gateway.EnsureContainerIsHealthyAsync(containerId, cancellationToken);
        }
    }

    public class FileTests : IClassFixture<SftpContainerFixture>
    {
        private readonly SftpContainerFixture _sftpContainerFixture;

        public FileTests(SftpContainerFixture sftpContainerFixture)
        {
            _sftpContainerFixture = sftpContainerFixture;
        }

        [Fact]
        public async Task CreateFileAsync_WhenTheFileDoesNotExist_ExpectFileToBeCreated()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            // Act
            await containerGateway.CreateFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }
        }

        [Fact]
        public async Task CreateFileAsync_WhenTheFileExists_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            await containerGateway.CreateFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => containerGateway.CreateFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken));
        }

        [Fact]
        public async Task CreateFileAsync_WhenThePathIncludesNewDirectories_ExpectFileToBeCreated()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}/{Guid.NewGuid()}/{Guid.NewGuid()}";

            // Act
            await containerGateway.CreateFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }
        }

        [Fact]
        public async Task DeleteFileAsync_WhenTheFileDoesNotExist_ExpectFileToBeDeleted()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }

            // Act
            await containerGateway.DeleteFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }
        }

        [Fact]
        public async Task DeleteFileAsync_WhenTheFileExists_ExpectNothingToHappen()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            await containerGateway.CreateFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }

            // Act
            await containerGateway.DeleteFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            var result = string.Empty;

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }
        }

        [Fact]
        public async Task WriteFileAsync_WhenTheFileDoesNotExist_ExpectFileToBeWritten()
        {
            // Arrange
            var expectedContents = "Hello, world";
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }

            // Act
            await containerGateway.WriteFileAsync(_sftpContainerFixture.ContainerId, path, expectedContents, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} cat {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualContents = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Equal(expectedContents, actualContents);
            }
        }

        [Fact]
        public async Task WriteFileAsync_WhenTheFileExists_ExpectFileToBeOverwritten()
        {
            // Arrange
            var expectedContents = "Hello, world";
            var originalContents = "Oh no";
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            await containerGateway.WriteFileAsync(_sftpContainerFixture.ContainerId, path, originalContents, cancellationToken);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} cat {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var contents = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Equal(originalContents, contents);
            }

            // Act
            await containerGateway.WriteFileAsync(_sftpContainerFixture.ContainerId, path, expectedContents, cancellationToken);

            // Assert
            var result = string.Empty;

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} cat {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualContents = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Equal(expectedContents, actualContents);
            }
        }

        [Fact]
        public async Task WriteFileAsync_WhenTheFileIsForADirectoryThatDoesNotExist_ExpectFileToBeWritten()
        {
            // Arrange
            var expectedContents = "Hello, world";
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}/{Guid.NewGuid()}";

            // Act
            await containerGateway.WriteFileAsync(_sftpContainerFixture.ContainerId, path, expectedContents, cancellationToken);

            // Assert
            var result = string.Empty;

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} cat {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualContents = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Equal(expectedContents, actualContents);
            }
        }

        [Fact]
        public async Task AppendFileAsync_WhenTheFileDoesNotExist_ExpectFileToBeWritten()
        {
            // Arrange
            var expectedContents = "Hello, world";
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }

            // Act
            await containerGateway.AppendFileAsync(_sftpContainerFixture.ContainerId, path, expectedContents, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} cat {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualContents = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Equal(expectedContents, actualContents);
            }
        }

        [Fact]
        public async Task AppendFileAsync_WhenTheFileExists_ExpectFileToBeAppended()
        {
            // Arrange
            var newContents = "Hello, world";
            var originalContents = "Oh no";
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";
            var expectedContents = $"{originalContents}{newContents}";

            await containerGateway.WriteFileAsync(_sftpContainerFixture.ContainerId, path, originalContents, cancellationToken);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} cat {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var contents = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Equal(originalContents, contents);
            }

            // Act
            await containerGateway.AppendFileAsync(_sftpContainerFixture.ContainerId, path, newContents, cancellationToken);

            // Assert
            var result = string.Empty;

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} cat {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualContents = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Equal(expectedContents, actualContents);
            }
        }

        [Fact]
        public async Task AppendFileAsync_WhenTheFileIsForADirectoryThatDoesNotExist_ExpectFileToBeWritten()
        {
            // Arrange
            var expectedContents = "Hello, world";
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}/{Guid.NewGuid()}";

            // Act
            await containerGateway.AppendFileAsync(_sftpContainerFixture.ContainerId, path, expectedContents, cancellationToken);

            // Assert
            var result = string.Empty;

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} cat {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var actualContents = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Equal(expectedContents, actualContents);
            }
        }

        [Fact]
        public async Task ReadFileAsync_WhenTheFileDoesNotExist_ExpectFileToBeWritten()
        {
            // Arrange
            var expectedContents = string.Empty;
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }

            // Act
            var actualContents = await containerGateway.ReadFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            Assert.Equal(expectedContents, actualContents);
        }

        [Fact]
        public async Task ReadFileAsync_WhenTheFileExists_ExpectFileToBeAppended()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";
            var expectedContents = "Hello, world";

            await containerGateway.WriteFileAsync(_sftpContainerFixture.ContainerId, path, expectedContents, cancellationToken);

            // Act
            var actualContents = await containerGateway.ReadFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            Assert.Equal(expectedContents, actualContents);
        }
    }

    public class DirectoryTests : IClassFixture<SftpContainerFixture>
    {
        private readonly SftpContainerFixture _sftpContainerFixture;

        public DirectoryTests(SftpContainerFixture sftpContainerFixture)
        {
            _sftpContainerFixture = sftpContainerFixture;
        }

        [Fact]
        public async Task CreateDirectoryAsync_WhenDirectoryDoesNotExist_ExpectDirectoryToBeCreated()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            // Act
            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }
        }

        [Fact]
        public async Task CreateDirectoryAsync_WhenPathExists_ExpectExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, path, cancellationToken));
        }

        [Fact]
        public async Task CreateDirectoryAsync_WhenParentDirectoryDoesNotExist_ExpectDirectoryToBeCreated()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}/{Guid.NewGuid()}";

            // Act
            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }
        }

        [Fact]
        public async Task DeleteDirectoryAsync_WhenDirectoryDoesNotExist_ExpectDirectoryToStillNotExist()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            // Act
            await containerGateway.DeleteDirectoryAsync(_sftpContainerFixture.ContainerId, path, false, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }
        }

        [Fact]
        public async Task DeleteDirectoryAsync_WhenPathExists_ExpectDirectoryToBeRemoved()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }

            // Act
            await containerGateway.DeleteDirectoryAsync(_sftpContainerFixture.ContainerId, path, false, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }
        }

        [Fact]
        public async Task DeleteDirectoryAsync_WhenPathExistsAndHasChildrenAndRequestIsNotRecursive_ExpectExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";
            var childPath = $"{path}/{Guid.NewGuid()}";

            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, childPath, cancellationToken);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {childPath}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => containerGateway.DeleteDirectoryAsync(_sftpContainerFixture.ContainerId, path, false, cancellationToken));
        }

        [Fact]
        public async Task DeleteDirectoryAsync_WhenPathExistsAndHasChildrenAndRequestIsRecursive_ExpectDirectoryToBeRemoved()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";
            var childPath = $"{path}/{Guid.NewGuid()}";

            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, childPath, cancellationToken);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {childPath}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }

            // Act
            await containerGateway.DeleteDirectoryAsync(_sftpContainerFixture.ContainerId, path, true, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var temp = process.StandardOutput.ReadToEnd();

                Assert.Equal(1, process.ExitCode);
            }
        }

        [Fact]
        public async Task EnumerateDirectories_WhenDirectoryDoesNotExist_ExpectReturnEmptyEnumerable()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }

            // Act
            var results = await containerGateway.EnumerateDirectoriesAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task EnumerateDirectories_WhenThereAreNoSubdirectories_ExpectReturnEmptyEnumerable()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }

            // Act
            var results = await containerGateway.EnumerateDirectoriesAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task EnumerateDirectories_WhenThereAreSubdirectoriesAndFiles_ExpectReturnSubdirectories()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";
            var subdirectoryPath1 = $"{path}/{Guid.NewGuid()}";
            var subdirectoryPath2 = $"{path}/{Guid.NewGuid()}";

            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, subdirectoryPath1, cancellationToken);
            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, subdirectoryPath2, cancellationToken);

            // Act
            var results = await containerGateway.EnumerateDirectoriesAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            Assert.Contains(results, x => x.Path == subdirectoryPath1);
            Assert.Contains(results, x => x.Path == subdirectoryPath2);
        }

        [Fact]
        public async Task EnumerateFiles_WhenDirectoryDoesNotExist_ExpectReturnEmptyEnumerable()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }

            // Act
            var results = await containerGateway.EnumerateFilesAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task EnumerateFiles_WhenThereAreNoFiles_ExpectReturnEmptyEnumerable()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            await containerGateway.CreateDirectoryAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(0, process.ExitCode);
            }

            // Act
            var results = await containerGateway.EnumerateFilesAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task EnumerateFiles_WhenThereAreFiles_ExpectReturnFiles()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";
            var filePath1 = $"{path}/{Guid.NewGuid()}";
            var filePath2 = $"{path}/{Guid.NewGuid()}";

            await containerGateway.CreateFileAsync(_sftpContainerFixture.ContainerId, filePath1, cancellationToken);
            await containerGateway.CreateFileAsync(_sftpContainerFixture.ContainerId, filePath2, cancellationToken);

            // Act
            var results = await containerGateway.EnumerateFilesAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Assert
            Assert.Contains(results, x => x.Path == filePath1);
            Assert.Contains(results, x => x.Path == filePath2);
        }
    }

    public class FileSystemTests : IClassFixture<SftpContainerFixture>
    {
        private readonly SftpContainerFixture _sftpContainerFixture;

        public FileSystemTests(SftpContainerFixture sftpContainerFixture)
        {
            _sftpContainerFixture = sftpContainerFixture;
        }

        [Fact]
        public async Task SetFileSystemResourceOwnerAsync_WhenTheResourceDoesNotExist_ExpectExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => containerGateway.SetFileSystemResourceOwnerAsync(_sftpContainerFixture.ContainerId, path, "root", cancellationToken));
        }

        [Theory]
        [InlineData("guest")]
        [InlineData("admin")]
        [InlineData("other")]
        public async Task SetFileSystemResourceOwnerAsync_WhenTheResourceDoesExists_ExpectTheOwnerToBeUpdated(string expectedOwner)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            await containerGateway.CreateFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Act
            await containerGateway.SetFileSystemResourceOwnerAsync(_sftpContainerFixture.ContainerId, path, expectedOwner, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} stat -c \"%U\" {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                var actualOwner = process.StandardOutput.ReadLine();

                Assert.Equal(expectedOwner, actualOwner);
            }
        }

        [Fact]
        public async Task SetFileSystemResourcePermissionsAsync_WhenTheResourceDoesNotExist_ExpectExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} ls -l {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                Assert.Equal(1, process.ExitCode);
            }

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => containerGateway.SetFileSystemResourcePermissionsAsync(_sftpContainerFixture.ContainerId, path, "777", cancellationToken));
        }

        [Theory]
        [InlineData("777")]
        [InlineData("757")]
        [InlineData("555")]
        public async Task SetFileSystemResourcePermissionsAsync_WhenTheResourceDoesExists_ExpectThePermissionsToBeUpdated(string expectedPermissions)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var containerGateway = new ContainerGateway();
            var path = $"/tmp/{Guid.NewGuid()}";

            await containerGateway.CreateFileAsync(_sftpContainerFixture.ContainerId, path, cancellationToken);

            // Act
            await containerGateway.SetFileSystemResourcePermissionsAsync(_sftpContainerFixture.ContainerId, path, expectedPermissions, cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"exec {_sftpContainerFixture.ContainerId} stat -c \"%a\" {path}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                var actualPermissions = process.StandardOutput.ReadLine();

                Assert.Equal(expectedPermissions, actualPermissions);
            }
        }
    }
}

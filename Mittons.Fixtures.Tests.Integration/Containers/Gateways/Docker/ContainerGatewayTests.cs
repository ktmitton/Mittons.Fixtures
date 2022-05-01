using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Gateways.Docker;
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
            var containerId = await gateway.CreateContainerAsync(expectedImageName, labels, command, cancellationToken).ConfigureAwait(false);

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
            var containerId = await gateway.CreateContainerAsync(imageName, expectedLabels, command, cancellationToken).ConfigureAwait(false);

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

        public LifetimeTests(DockerCleanupFixture dockerCleanupFixture)
        {
            _dockerCleanupFixture = dockerCleanupFixture;
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

            var containerId = await gateway.CreateContainerAsync(imageName, labels, command, cancellationToken).ConfigureAwait(false);

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
            var containerId = await gateway.CreateContainerAsync(imageName, labels, command, cancellationToken).ConfigureAwait(false);

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
        [InlineData("tcp", 6379)]
        [InlineData("tcp", 80)]
        [InlineData("tcp", 443)]
        [InlineData("tcp", 22)]
        [InlineData("udp", 2834)]
        public async Task GetAvailableResourcesAsync_WhenAnImageNameHasAnExposedPort_ExpectThePortToBeAddedAsAResource(string scheme, int guestPort)
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
                process.StartInfo.Arguments = $"run -d -p 6379/tcp -p 80/tcp -p 443/tcp -p 22/tcp -p 2834/udp redis:alpine";
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
    }
}

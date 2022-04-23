using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Exceptions.Containers;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration;

public class GuestEnvironmentFixtureTests
{
    public class RemoveServiceTests : IDisposable
    {
        private readonly List<string> _containerIds = new List<string>();

        private readonly CancellationToken _cancellationToken;

        public RemoveServiceTests()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

            _cancellationToken = cancellationTokenSource.Token;
        }

        public void Dispose()
        {
            foreach (var containerId in _containerIds)
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"rm --force {containerId}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();
            }
        }

        [Fact]
        public async Task RemoveServiceAsync_WhenCalled_ExpectTheServiceToBeRemoved()
        {
            // Arrange
            var attributes = new[] { new ImageAttribute("alpine:3.15") };

            var serviceGateway = new DockerServiceGateway();

            var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken);

            _containerIds.Add(service.ContainerId);

            // Act
            await serviceGateway.RemoveServiceAsync(service, _cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"ps -a --filter id={service.ContainerId} --format \"{{{{.ID}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                var output = process.StandardOutput.ReadToEnd();
                Assert.Empty(output);
            }
        }
    }

    public class CreateServiceTests : IDisposable
    {
        private readonly List<string> _containerIds = new List<string>();

        private readonly CancellationToken _cancellationToken;

        public CreateServiceTests()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

            _cancellationToken = cancellationTokenSource.Token;
        }

        public void Dispose()
        {
            foreach (var containerId in _containerIds)
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"rm --force {containerId}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();
            }
        }

        [Fact]
        public async Task CreateServiceAsync_WhenCalledWithoutAnImageName_ExpectAnErrorToBeThrown()
        {
            // Arrange
            var attributes = Enumerable.Empty<Attribute>();
            var serviceGateway = new DockerServiceGateway();

            // Act
            // Assert
            await Assert.ThrowsAsync<ImageNameMissingException>(() => serviceGateway.CreateServiceAsync(attributes, _cancellationToken));
        }

        [Fact]
        public async Task CreateServiceAsync_WhenCalledWitMultipleImageNames_ExpectAnErrorToBeThrown()
        {
            // Arrange
            var attributes = new[] { new ImageAttribute("alpine:3.15"), new ImageAttribute("alpine:3.14") };
            var serviceGateway = new DockerServiceGateway();

            // Act
            // Assert
            await Assert.ThrowsAsync<MultipleImageNamesProvidedException>(() => serviceGateway.CreateServiceAsync(attributes, _cancellationToken));
        }

        [Theory]
        [InlineData("alpine:3.15")]
        [InlineData("alpine:3.14")]
        public async Task InitializedServiceAsync_WhenCalledWithAnImageName_ExpectTheImageToBeCreated(string imageName)
        {
            // Arrange
            var attributes = new[] { new ImageAttribute(imageName) };
            var serviceGateway = new DockerServiceGateway();

            // Act
            var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken);
            _containerIds.Add(service.ContainerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {service.ContainerId} --format \"{{{{.Config.Image}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                var output = process.StandardOutput.ReadLine();

                Assert.Equal(imageName, output);
            }
        }

        [Theory]
        [InlineData("test")]
        [InlineData("test test2")]
        public async Task InitializedServiceAsync_WhenCalledWithACommand_ExpectTheContainerToApplyTheCommand(string command)
        {
            // Arrange
            var commandParts = command.Split(" ");

            var attributes = new List<Attribute> { new ImageAttribute("alpine:3.15") };
            attributes.AddRange(commandParts.Select(x => new CommandAttribute(x)));

            var serviceGateway = new DockerServiceGateway();

            // Act
            var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken);
            _containerIds.Add(service.ContainerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {service.ContainerId} --format \"{{{{json .Config.Cmd}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                var output = JsonSerializer.Deserialize<string[]>(await process.StandardOutput.ReadToEndAsync()) ?? new string[0];

                Assert.Equal(commandParts.Length, output.Length);
                Assert.All(commandParts, x => Assert.Contains(x, output));
            }
        }

        [Theory]
        [InlineData("atmoz/sftp:alpine", "guest:guest", 22, "tcp")]
        [InlineData("redis:alpine", "", 6379, "tcp")]
        public async Task InitializedServiceAsync_WhenCalledForAnImageWithAnExposedPort_ExpectPortToBePublished(string imageName, string command, int port, string scheme)
        {
            // Arrange
            var attributes = new Attribute[] { new ImageAttribute(imageName), new CommandAttribute(command) };
            var serviceGateway = new DockerServiceGateway();

            var expectedUriBuilder = new UriBuilder();
            expectedUriBuilder.Scheme = scheme;

            // Act
            var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken);
            _containerIds.Add(service.ContainerId);

            // Assert
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                expectedUriBuilder.Host = "localhost";

                using (var portProcess = new Process())
                {
                    portProcess.StartInfo.FileName = "docker";
                    portProcess.StartInfo.Arguments = $"port {service.ContainerId} {port}";
                    portProcess.StartInfo.UseShellExecute = false;
                    portProcess.StartInfo.RedirectStandardOutput = true;

                    portProcess.Start();
                    portProcess.WaitForExit();

                    var portDetails = (await portProcess.StandardOutput.ReadLineAsync() ?? string.Empty).Split(":");

                    Assert.Equal(2, portDetails.Length);

                    expectedUriBuilder.Port = int.Parse(portDetails[1]);
                }
            }
            else
            {
                using (var ipProcess = new Process())
                {
                    ipProcess.StartInfo.FileName = "docker";
                    ipProcess.StartInfo.Arguments = $"inspect {service.ContainerId} --format \"{{{{.NetworkSettings.IPAddress}}}}\"";
                    ipProcess.StartInfo.UseShellExecute = false;
                    ipProcess.StartInfo.RedirectStandardOutput = true;

                    ipProcess.Start();
                    ipProcess.WaitForExit();

                    expectedUriBuilder.Host = await ipProcess.StandardOutput.ReadLineAsync();
                }

                expectedUriBuilder.Port = port;
            }

            Assert.Equal(expectedUriBuilder.Uri, service.Resources.Single(x => x.GuestUri.Scheme == scheme && x.GuestUri.Port == port).HostUri);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Gateways.Docker;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration.Containers.Gateways;

public class ContainerNetworkGatewayTests
{
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
            var expectedLabels = new Dictionary<string, string>
            {
                { "mylabel", "myvalue" },
                { "myotherlabel", "myothervalue" }
            };

            var cancellationToken = new CancellationTokenSource().Token;

            var networkName = Guid.NewGuid().ToString();

            var gateway = new ContainerNetworkGateway();

            // Act
            var networkId = await gateway.CreateNetworkAsync(networkName, expectedLabels, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddNetwork(networkId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network inspect {networkId} --format \"{{{{json .Labels}}}}\"";
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
        public async Task CreateNetworkAsync_WhenCalled_ExpectTheNetworkToBeCreated()
        {
            // Arrange
            var labels = new Dictionary<string, string>();

            var cancellationToken = new CancellationTokenSource().Token;

            var networkName = Guid.NewGuid().ToString();

            var gateway = new ContainerNetworkGateway();

            // Act
            var networkId = await gateway.CreateNetworkAsync(networkName, labels, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddNetwork(networkId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network ls -qf id={networkId}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.NotEmpty(output);
            }
        }

        [Fact]
        public async Task RemoveNetworkAsync_WhenCalled_ExpectTheNetworkToBeRemoved()
        {
            // Arrange
            var labels = new Dictionary<string, string>();

            var cancellationToken = new CancellationTokenSource().Token;

            var networkName = Guid.NewGuid().ToString();

            var gateway = new ContainerNetworkGateway();

            var networkId = await gateway.CreateNetworkAsync(networkName, labels, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddNetwork(networkId);

            // Act
            await gateway.RemoveNetworkAsync(networkId, cancellationToken).ConfigureAwait(false);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network ls -qf id={networkId}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Empty(output);
            }
        }

        [Fact]
        public async Task RemoveNetworkAsync_WhenNetworkHasConnectedContainers_ExpectNetworkToBeRemovedButTheContainerToRemain()
        {
            // Arrange
            var labels = new Dictionary<string, string>();

            var cancellationToken = new CancellationTokenSource().Token;

            var networkName = Guid.NewGuid().ToString();

            var gateway = new ContainerNetworkGateway();

            var networkId = await gateway.CreateNetworkAsync(networkName, labels, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddNetwork(networkId);

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

            var alias = "primary.example.com";

            await gateway.ConnectAsync(networkId, containerId, alias, cancellationToken).ConfigureAwait(false);

            // Act
            await gateway.RemoveNetworkAsync(networkId, cancellationToken).ConfigureAwait(false);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network ls -qf id={networkId}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Empty(output);
            }
        }
    }

    public class ConnectedServiceTests : IClassFixture<DockerCleanupFixture>
    {
        private readonly DockerCleanupFixture _dockerCleanupFixture;

        public ConnectedServiceTests(DockerCleanupFixture dockerCleanupFixture)
        {
            _dockerCleanupFixture = dockerCleanupFixture;
        }

        [Fact]
        public async Task CreateNetworkAsync_WhenCalled_ExpectTheNetworkToBeCreated()
        {
            // Arrange
            var labels = new Dictionary<string, string>();

            var cancellationToken = new CancellationTokenSource().Token;

            var networkName = Guid.NewGuid().ToString();

            var gateway = new ContainerNetworkGateway();

            var networkId = await gateway.CreateNetworkAsync(networkName, labels, cancellationToken).ConfigureAwait(false);

            _dockerCleanupFixture.AddNetwork(networkId);

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

            var expectedAlias = "primary.example.com";

            // Act
            await gateway.ConnectAsync(networkId, containerId, expectedAlias, cancellationToken).ConfigureAwait(false);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{json .NetworkSettings.Networks}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

                var networks = JsonSerializer.Deserialize<Dictionary<string, Network>>(output ?? string.Empty);

                Assert.Contains(networks, x => x.Key.StartsWith(networkName) && x.Value.Aliases.Contains(expectedAlias));
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"ps -a --filter id={containerId} --format \"{{{{.ID}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.NotEmpty(output);
            }
        }

        private record Network(string[] Aliases);
    }
}

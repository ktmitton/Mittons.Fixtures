using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Attributes;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Exceptions;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration.Containers.Gateways;

public class DockerNetworkGatewayTests
{
    public class RemoveServiceTests : Xunit.IAsyncLifetime
    {
        private readonly List<string> _networkIds = new List<string>();

        private readonly CancellationToken _cancellationToken;

        public RemoveServiceTests()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

            _cancellationToken = cancellationTokenSource.Token;
        }

        public Task InitializeAsync()
            => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            foreach (var networkId in _networkIds)
            {
                using var process = new Process();
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network rm {networkId}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task RemoveServiceAsync_WhenCalled_ExpectTheServiceToBeRemoved()
        {
            // Arrange
            var attributes = new Attribute[] { new NetworkAttribute("test"), new RunAttribute() };

            var networkGateway = new DockerNetworkGateway();

            var network = await networkGateway.CreateNetworkAsync(attributes, _cancellationToken);

            _networkIds.Add(network.NetworkId);

            // Act
            await networkGateway.RemoveNetworkAsync(network, _cancellationToken).ConfigureAwait(false);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network ls -qf id={network.NetworkId}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                Assert.Empty(output);
            }
        }
    }

    public class OptionTests : Xunit.IAsyncLifetime
    {
        private readonly List<string> _networkIds = new List<string>();

        private readonly CancellationToken _cancellationToken;

        public OptionTests()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

            _cancellationToken = cancellationTokenSource.Token;
        }

        public Task InitializeAsync()
            => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            foreach (var networkId in _networkIds)
            {
                using var process = new Process();
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network rm {networkId}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateNetworkAsync_WhenCalled_ExpectRunOptionsToBeApplied()
        {
            // Arrange
            var attributes = new Attribute[] { new NetworkAttribute("test"), new RunAttribute() };
            var networkGateway = new DockerNetworkGateway();

            // Act
            var network = await networkGateway.CreateNetworkAsync(attributes, _cancellationToken).ConfigureAwait(false);
            _networkIds.Add(network.NetworkId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network inspect {network.NetworkId} --format \"{{{{json .Labels}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

                Assert.Contains(labels, x => x.Key == "mittons.fixtures.run.id" && x.Value == RunAttribute.DefaultId);
            }
        }
    }

    public class CreateNetworkTests : Xunit.IAsyncLifetime
    {
        private readonly List<string> _networkIds = new List<string>();

        private readonly CancellationToken _cancellationToken;

        public CreateNetworkTests()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

            _cancellationToken = cancellationTokenSource.Token;
        }

        public Task InitializeAsync()
            => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            foreach (var networkId in _networkIds)
            {
                using var process = new Process();
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network rm {networkId}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateNetworkAsync_WhenCalledWithoutAName_ExpectAnErrorToBeThrown()
        {
            // Arrange
            var attributes = Enumerable.Empty<Attribute>();
            var networkGateway = new DockerNetworkGateway();

            // Act
            // Assert
            await Assert.ThrowsAsync<NetworkNameMissingException>(() => networkGateway.CreateNetworkAsync(attributes, _cancellationToken)).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateNetworkAsync_WhenCalledWitMultipleNetworkNames_ExpectAnErrorToBeThrown()
        {
            // Arrange
            var attributes = new[] { new NetworkAttribute("network1"), new NetworkAttribute("network1") };
            var networkGateway = new DockerNetworkGateway();

            // Act
            // Assert
            await Assert.ThrowsAsync<MultipleNetworkNamesProvidedException>(() => networkGateway.CreateNetworkAsync(attributes, _cancellationToken)).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("network1")]
        [InlineData("network2")]
        public async Task CreateNetworkAsync_WhenCalledWithANetworkName_ExpectTheNetworkToBeCreated(string networkName)
        {
            // Arrange
            var attributes = new Attribute[] { new NetworkAttribute(networkName), new RunAttribute() };
            var networkGateway = new DockerNetworkGateway();

            // Act
            var network = await networkGateway.CreateNetworkAsync(attributes, _cancellationToken).ConfigureAwait(false);
            _networkIds.Add(network.NetworkId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"network inspect {network.NetworkId} --format \"{{{{.Name}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);

                var output = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

                Assert.StartsWith(networkName, output);
            }
        }
    }
}

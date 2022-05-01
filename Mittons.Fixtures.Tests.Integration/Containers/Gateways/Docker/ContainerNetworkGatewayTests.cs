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

public class ContainerNetworkGatewayTests
{
}
//     public class RemoveServiceTests : Xunit.IAsyncLifetime
//     {
//         private readonly List<string> _networkIds = new List<string>();

//         private readonly CancellationToken _cancellationToken;

//         public RemoveServiceTests()
//         {
//             var cancellationTokenSource = new CancellationTokenSource();
//             cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

//             _cancellationToken = cancellationTokenSource.Token;
//         }

//         public Task InitializeAsync()
//             => Task.CompletedTask;

//         public async Task DisposeAsync()
//         {
//             foreach (var networkId in _networkIds)
//             {
//                 using var process = new Process();
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"network rm {networkId}";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);
//             }
//         }

//         [Fact]
//         public async Task RemoveServiceAsync_WhenCalled_ExpectTheServiceToBeRemoved()
//         {
//             // Arrange
//             var attributes = new Attribute[] { new NetworkAttribute("test"), new RunAttribute() };

//             var networkGateway = new DockerNetworkGateway();

//             var network = await networkGateway.CreateNetworkAsync(attributes, _cancellationToken);

//             _networkIds.Add(network.NetworkId);

//             // Act
//             await networkGateway.RemoveNetworkAsync(network, _cancellationToken).ConfigureAwait(false);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"network ls -qf id={network.NetworkId}";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

//                 Assert.Empty(output);
//             }
//         }
//     }

//     public class OptionTests : Xunit.IAsyncLifetime
//     {
//         private readonly List<string> _networkIds = new List<string>();

//         private readonly CancellationToken _cancellationToken;

//         public OptionTests()
//         {
//             var cancellationTokenSource = new CancellationTokenSource();
//             cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

//             _cancellationToken = cancellationTokenSource.Token;
//         }

//         public Task InitializeAsync()
//             => Task.CompletedTask;

//         public async Task DisposeAsync()
//         {
//             foreach (var networkId in _networkIds)
//             {
//                 using var process = new Process();
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"network rm {networkId}";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);
//             }
//         }

//         [Fact]
//         public async Task CreateNetworkAsync_WhenCalled_ExpectRunOptionsToBeApplied()
//         {
//             // Arrange
//             var attributes = new Attribute[] { new NetworkAttribute("test"), new RunAttribute() };
//             var networkGateway = new DockerNetworkGateway();

//             // Act
//             var network = await networkGateway.CreateNetworkAsync(attributes, _cancellationToken).ConfigureAwait(false);
//             _networkIds.Add(network.NetworkId);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"network inspect {network.NetworkId} --format \"{{{{json .Labels}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

//                 Assert.Contains(labels, x => x.Key == "mittons.fixtures.run.id" && x.Value == RunAttribute.DefaultId);
//             }
//         }
//     }

//     public class CreateNetworkTests : Xunit.IAsyncLifetime
//     {
//         private readonly List<string> _networkIds = new List<string>();

//         private readonly CancellationToken _cancellationToken;

//         public CreateNetworkTests()
//         {
//             var cancellationTokenSource = new CancellationTokenSource();
//             cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

//             _cancellationToken = cancellationTokenSource.Token;
//         }

//         public Task InitializeAsync()
//             => Task.CompletedTask;

//         public async Task DisposeAsync()
//         {
//             foreach (var networkId in _networkIds)
//             {
//                 using var process = new Process();
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"network rm {networkId}";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);
//             }
//         }

//         [Fact]
//         public async Task CreateNetworkAsync_WhenCalledWithoutAName_ExpectAnErrorToBeThrown()
//         {
//             // Arrange
//             var attributes = Enumerable.Empty<Attribute>();
//             var networkGateway = new DockerNetworkGateway();

//             // Act
//             // Assert
//             await Assert.ThrowsAsync<NetworkNameMissingException>(() => networkGateway.CreateNetworkAsync(attributes, _cancellationToken)).ConfigureAwait(false);
//         }

//         [Fact]
//         public async Task CreateNetworkAsync_WhenCalledWitMultipleNetworkNames_ExpectAnErrorToBeThrown()
//         {
//             // Arrange
//             var attributes = new[] { new NetworkAttribute("network1"), new NetworkAttribute("network1") };
//             var networkGateway = new DockerNetworkGateway();

//             // Act
//             // Assert
//             await Assert.ThrowsAsync<MultipleNetworkNamesProvidedException>(() => networkGateway.CreateNetworkAsync(attributes, _cancellationToken)).ConfigureAwait(false);
//         }

//         [Theory]
//         [InlineData("network1")]
//         [InlineData("network2")]
//         public async Task CreateNetworkAsync_WhenCalledWithANetworkName_ExpectTheNetworkToBeCreated(string networkName)
//         {
//             // Arrange
//             var attributes = new Attribute[] { new NetworkAttribute(networkName), new RunAttribute() };
//             var networkGateway = new DockerNetworkGateway();

//             // Act
//             var network = await networkGateway.CreateNetworkAsync(attributes, _cancellationToken).ConfigureAwait(false);
//             _networkIds.Add(network.NetworkId);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"network inspect {network.NetworkId} --format \"{{{{.Name}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var output = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

//                 Assert.StartsWith(networkName, output);
//             }
//         }
//     }

//     public class ConnectServiceTests : Xunit.IAsyncLifetime
//     {
//         private readonly List<string> _containerIds = new List<string>();

//         private readonly List<string> _networkIds = new List<string>();

//         private readonly CancellationToken _cancellationToken;

//         public ConnectServiceTests()
//         {
//             var cancellationTokenSource = new CancellationTokenSource();
//             cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

//             _cancellationToken = cancellationTokenSource.Token;
//         }

//         public Task InitializeAsync()
//             => Task.CompletedTask;

//         public async Task DisposeAsync()
//         {
//             foreach (var containerId in _containerIds)
//             {
//                 using var process = new Process();
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"rm --force {containerId}";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);
//             }

//             foreach (var networkId in _networkIds)
//             {
//                 using var process = new Process();
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"network rm {networkId}";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);
//             }
//         }

//         [Fact]
//         public async Task ConnectServiceAsync_WhenCalledForAnUnsupportedService_ExpectAnErrorToBeThrown()
//         {
//             // Arrange
//             var service = new GenericService(string.Empty, Enumerable.Empty<IResource>());

//             var networkattributes = new Attribute[] { new NetworkAttribute("test"), new RunAttribute() };
//             var networkGateway = new DockerNetworkGateway();
//             var network = await networkGateway.CreateNetworkAsync(networkattributes, _cancellationToken).ConfigureAwait(false);
//             _networkIds.Add(network.NetworkId);

//             // Act
//             // Assert
//             await Assert.ThrowsAsync<NotSupportedException>(() => networkGateway.ConnectServiceAsync(network, service, new NetworkAliasAttribute(string.Empty, string.Empty), _cancellationToken)).ConfigureAwait(false);
//         }

//         [Theory]
//         [InlineData("network1", "redis.example.com")]
//         [InlineData("network2", "cache.example.com")]
//         public async Task ConnectServiceAsync_WhenCalledForASupportedService_ExpectTheServiceToBeConnectedToTheNetwork(string networkName, string aliasName)
//         {
//             // Arrange
//             var alias = new NetworkAliasAttribute(networkName, aliasName);

//             var serviceAttributes = new Attribute[] { new ImageAttribute("redis:alpine"), new RunAttribute() };
//             var serviceGateway = new DockerServiceGateway();
//             var service = await serviceGateway.CreateServiceAsync(serviceAttributes, _cancellationToken).ConfigureAwait(false);
//             _containerIds.Add(service.ServiceId);

//             var networkattributes = new Attribute[] { new NetworkAttribute(networkName), new RunAttribute() };
//             var networkGateway = new DockerNetworkGateway();
//             var network = await networkGateway.CreateNetworkAsync(networkattributes, _cancellationToken).ConfigureAwait(false);
//             _networkIds.Add(network.NetworkId);

//             // Act
//             await networkGateway.ConnectServiceAsync(network, service, alias, _cancellationToken).ConfigureAwait(false);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"inspect {service.ServiceId} --format \"{{{{json .NetworkSettings.Networks}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var output = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

//                 var networks = JsonSerializer.Deserialize<Dictionary<string, Network>>(output ?? string.Empty);

//                 Assert.Contains(networks, x => x.Key.StartsWith(networkName) && x.Value.Aliases.Contains(aliasName));
//             }
//         }

//         private record GenericService(string ServiceId, IEnumerable<IResource> Resources) : IService;

//         private record Network(string[] Aliases);
//     }
// }

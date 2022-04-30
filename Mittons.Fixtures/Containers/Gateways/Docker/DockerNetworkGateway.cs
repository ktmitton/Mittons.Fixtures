// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Mittons.Fixtures.Attributes;
// using Mittons.Fixtures.Exceptions;
// using Mittons.Fixtures.Extensions;
// using Mittons.Fixtures.Models;

// namespace Mittons.Fixtures.Containers.Gateways
// {
//     public class DockerNetworkGateway : INetworkGateway<IContainerNetwork>
//     {
//         /// <inheritdoc/>
//         public async Task<IContainerNetwork> CreateNetworkAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
//         {
//             var networks = attributes.OfType<NetworkAttribute>().ToArray();

//             if (networks.Length == 0)
//             {
//                 throw new NetworkNameMissingException();
//             }
//             else if (networks.Length > 1)
//             {
//                 throw new MultipleNetworkNamesProvidedException();
//             }

//             var networkName = $"{networks.First().Name}-{Guid.NewGuid()}";

//             var options = new List<Option>();

//             var run = attributes.OfType<RunAttribute>().Single();

//             options.Add(new Option { Name = "--label", Value = $"mittons.fixtures.run.id={run.Id}" });

//             using (var process = new DockerProcess($"network create {options.ToExecutionParametersFormattedString()} {networkName}"))
//             {
//                 await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

//                 var networkId = process.StandardOutput.ReadLine();

//                 return new ContainerNetwork(networkId, string.Empty);
//             }
//         }

//         /// <inheritdoc/>
//         public async Task RemoveNetworkAsync(IContainerNetwork network, CancellationToken cancellationToken)
//         {
//             using (var process = new DockerProcess($"network rm {network.NetworkId}"))
//             {
//                 await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
//             }
//         }

//         /// <inheritdoc/>
//         public async Task ConnectServiceAsync(IContainerNetwork network, IService service, NetworkAliasAttribute alias, CancellationToken cancellationToken)
//         {
//             if (!(service is IContainerService))
//             {
//                 throw new NotSupportedException($"Services of type [{service.GetType()}] are not supported by [{this.GetType()}].");
//             }

//             using (var process = new DockerProcess($"network connect --alias {alias.Alias} {network.NetworkId} {service.ServiceId}"))
//             {
//                 await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
//             }
//         }
//     }
// }

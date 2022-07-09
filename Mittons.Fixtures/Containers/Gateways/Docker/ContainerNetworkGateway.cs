using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Containers.Gateways.Docker
{
    internal class ContainerNetworkGateway : IContainerNetworkGateway
    {
        public async Task ConnectAsync(string networkId, string containerId, string alias, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"network connect --alias {alias} {networkId} {containerId}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<string> CreateNetworkAsync(string name, Dictionary<string, string> labels, CancellationToken cancellationToken)
        {
            var networkName = $"{name}-{Guid.NewGuid()}";

            var labelOptions = string.Join(" ", labels.Select(x => $"--label \"{x.Key}={x.Value}\""));

            using (var process = new DockerProcess($"network create {labelOptions} {networkName}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var networkId = process.StandardOutput.ReadLine();

                return networkId;
            }
        }

        public async Task RemoveNetworkAsync(string networkId, CancellationToken cancellationToken)
        {
            var connectedContainerIds = await GetConnectedContainersAsync(networkId, cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(connectedContainerIds.Select(x => DisconnectContainerAsync(networkId, x, cancellationToken))).ConfigureAwait(false);

            using (var process = new DockerProcess($"network rm {networkId}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<string>> GetConnectedContainersAsync(string networkId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"network inspect {networkId} --format \"{{{{json .Containers}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                var containers = string.IsNullOrWhiteSpace(output) ? new Dictionary<string, Dictionary<string, string>>() : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(output);

                return containers.Select(x => x.Key);
            }
        }

        private async Task DisconnectContainerAsync(string networkId, string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"network disconnect --force {networkId} {containerId}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Extensions;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Gateways
{
    public class NetworkGateway : INetworkGateway
    {
        private static TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

        public async Task CreateAsync(string networkName, IEnumerable<Option> options, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"network create {options.ToExecutionParametersFormattedString()} {networkName}"))
            {
                await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }
        }

        public async Task RemoveAsync(string networkName, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"network rm {networkName}"))
            {
                await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }
        }

        public async Task ConnectAsync(string networkName, string containerId, string alias, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"network connect --alias {alias} {networkName} {containerId}"))
            {
                await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }
        }
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Docker.Gateways
{
    public interface INetworkGateway
    {
        Task CreateAsync(string networkName, IEnumerable<KeyValuePair<string, string>> labels, CancellationToken cancellationToken);

        Task RemoveAsync(string networkName, CancellationToken cancellationToken);

        Task ConnectAsync(string networkName, string containerId, string alias, CancellationToken cancellationToken);
    }
}
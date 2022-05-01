using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Containers.Gateways
{
    public interface IContainerNetworkGateway
    {
        Task<string> CreateNetworkAsync(string name, Dictionary<string, string> labels, CancellationToken cancellationToken);

        Task RemoveNetworkAsync(string networkId, CancellationToken cancellationToken);

        Task ConnectAsync(string networkId, string containerId, string alias, CancellationToken cancellationToken);
    }
}

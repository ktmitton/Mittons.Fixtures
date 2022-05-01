using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Gateways
{
    public interface IContainerGateway
    {
        Task<string> CreateContainerAsync(Dictionary<string, string> labels, CancellationToken cancellationToken);

        Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken);

        Task<IEnumerable<IResource>> GetAvailableResourcesAsync(string containerId, CancellationToken cancellationToken);
    }
}

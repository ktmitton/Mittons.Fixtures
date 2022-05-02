using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Gateways
{
    public interface IContainerGateway
    {
        Task<string> CreateContainerAsync(string imageName, Dictionary<string, string> labels, string command, IHealthCheckDescription healthCheckDescription, CancellationToken cancellationToken);

        Task EnsureContainerIsHealthyAsync(string containerId, CancellationToken cancellationToken);

        Task<IEnumerable<IResource>> GetAvailableResourcesAsync(string containerId, CancellationToken cancellationToken);

        Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken);
    }
}

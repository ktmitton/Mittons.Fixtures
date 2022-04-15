using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IContainerGateway
    {
        Task<string> RunAsync(string imageName, string command, IEnumerable<Option> options, CancellationToken cancellationToken);

        Task RemoveAsync(string containerId, CancellationToken cancellationToken);

        Task<IPAddress> GetDefaultNetworkIpAddressAsync(string containerId, CancellationToken cancellationToken);

        Task AddFileAsync(string containerId, string hostFilename, string containerFilename, string owner, string permissions, CancellationToken cancellationToken);

        Task RemoveFileAsync(string containerId, string containerFilename, CancellationToken cancellationToken);

        Task<IEnumerable<string>> ExecuteCommandAsync(string containerId, string command, CancellationToken cancellationToken);

        Task<int> GetHostPortMappingAsync(string containerId, string protocol, int containerPort, CancellationToken cancellationToken);

        Task<HealthStatus> GetHealthStatusAsync(string containerId, CancellationToken cancellationToken);
    }
}
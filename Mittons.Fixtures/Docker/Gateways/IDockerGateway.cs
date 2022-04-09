using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IDockerGateway
    {
        string ContainerRun(string imageName, string command, Dictionary<string, string> labels);

        Task ContainerRemoveAsync(string containerId, CancellationToken cancellationToken);

        IPAddress ContainerGetDefaultNetworkIpAddress(string containerId);

        Task ContainerAddFileAsync(string containerId, string hostFilename, string containerFilename, string owner, string permissions, CancellationToken cancellationToken);

        Task ContainerRemoveFileAsync(string containerId, string containerFilename, CancellationToken cancellationToken);

        Task<IEnumerable<string>> ContainerExecuteCommandAsync(string containerId, string command, CancellationToken cancellationToken);

        Task<int> ContainerGetHostPortMappingAsync(string containerId, string protocol, int containerPort, CancellationToken cancellationToken);

        Task<HealthStatus> ContainerGetHealthStatusAsync(string containerId, CancellationToken cancellationToken);

        void NetworkCreate(string networkName, Dictionary<string, string> labels);

        Task NetworkRemoveAsync(string networkName, CancellationToken cancellationToken);

        Task NetworkConnectAsync(string networkName, string containerId, string alias, CancellationToken cancellationToken);
    }
}
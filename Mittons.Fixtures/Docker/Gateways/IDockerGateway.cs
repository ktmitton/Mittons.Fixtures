using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IDockerGateway
    {
        string ContainerRun(string imageName, string command, Dictionary<string, string> labels);

        void ContainerRemove(string containerId);

        IPAddress ContainerGetDefaultNetworkIpAddress(string containerId);

        Task ContainerAddFileAsync(string containerId, string hostFilename, string containerFilename, string owner, string permissions, CancellationToken cancellationToken);

        Task ContainerRemoveFileAsync(string containerId, string containerFilename, CancellationToken cancellationToken);

        IEnumerable<string> ContainerExecuteCommand(string containerId, string command);

        int ContainerGetHostPortMapping(string containerId, string protocol, int containerPort);

        Task<HealthStatus> ContainerGetHealthStatusAsync(string containerId, CancellationToken cancellationToken);

        void NetworkCreate(string networkName, Dictionary<string, string> labels);

        void NetworkRemove(string networkName);

        void NetworkConnect(string networkName, string containerId, string alias);
    }
}
using System.Net;

namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IDockerGateway
    {
        string ContainerRun(string imageName, string command);

        void ContainerRemove(string containerId);

        IPAddress ContainerGetDefaultNetworkIpAddress(string containerId);

        void NetworkCreate(string networkName);

        void NetworkRemove(string networkName);

        void NetworkConnect(string networkName, string containerId, string alias);
    }
}
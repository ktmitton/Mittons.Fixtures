using System.Net;

namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IDockerGateway
    {
        string ContainerRun(string imageName, string command);

        void ContainerRemove(string containerId);

        IPAddress ContainerGetDefaultNetworkIpAddress(string containerId);

        void ContainerAddFile(string containerId, string hostFilename, string containerFilename, string owner = null, string permissions = null);

        void ContainerRemoveFile(string containerId, string containerFilename);

        void NetworkCreate(string networkName);

        void NetworkRemove(string networkName);

        void NetworkConnect(string networkName, string containerId, string alias);
    }
}
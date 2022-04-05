namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IDockerGateway
    {
        string ContainerRun(string imageName, string command);

        void ContainerRemove(string containerId);

        void NetworkCreate(string name);

        void NetworkRemove(string name);

        void NetworkConnect(string networkName, string containerId, string alias);
    }
}
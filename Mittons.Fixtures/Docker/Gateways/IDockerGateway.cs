namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IDockerGateway
    {
        string Run(string imageName, string command);

        void Remove(string containerId);

        void CreateNetwork(string name);

        void RemoveNetwork(string name);

        void NetworkConnect(string networkName, string containerId, string alias);
    }
}
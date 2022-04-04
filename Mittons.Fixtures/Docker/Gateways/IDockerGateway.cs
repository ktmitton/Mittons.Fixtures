namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IDockerGateway
    {
        string Run(string imageName);

        void Remove(string containerName);
    }
}
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Networks
{
    public class DefaultNetwork
    {
        public DefaultNetwork(IDockerGateway dockerGateway, string name)
        {
            dockerGateway.CreateNetwork(name);
        }
    }
}
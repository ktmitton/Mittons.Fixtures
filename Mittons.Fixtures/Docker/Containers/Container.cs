using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Containers
{
    public class Container
    {
        public string Id { get; }

        private readonly IDockerGateway _dockerGateway;

        public Container(IDockerGateway dockerGateway, string imageName)
        {
            _dockerGateway = dockerGateway;

            Id = _dockerGateway.Run(imageName);
        }

        public void Dispose()
        {
            _dockerGateway.Remove(Id);
        }
    }
}
using System;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Containers
{
    public class Container : IDisposable
    {
        public string Id { get; }

        private readonly IDockerGateway _dockerGateway;

        public Container(IDockerGateway dockerGateway, string imageName)
        {
            _dockerGateway = dockerGateway;

            Id = _dockerGateway.Run(imageName);
        }

        public Container(IDockerGateway dockerGateway, string imageName, string command)
        {
            _dockerGateway = dockerGateway;

            Id = _dockerGateway.Run(imageName, command);
        }

        public void Dispose()
        {
            _dockerGateway.Remove(Id);
        }
    }
}
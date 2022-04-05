using System;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Networks
{
    public class DefaultNetwork : IDisposable
    {
        private readonly IDockerGateway _dockerGateway;

        private readonly string _name;

        public DefaultNetwork(IDockerGateway dockerGateway, string name)
        {
            _dockerGateway = dockerGateway;
            _name = name;

            _dockerGateway.NetworkCreate(_name);
        }

        public void Dispose()
        {
            _dockerGateway.NetworkRemove(_name);
        }
    }
}
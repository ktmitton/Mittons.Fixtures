using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Containers
{
    public class Container : IDisposable
    {
        public string Id { get; }

        public IPAddress IpAddress { get; set; }

        private readonly IDockerGateway _dockerGateway;

        public Container(IDockerGateway dockerGateway, IEnumerable<Attribute> attributes)
        {
            _dockerGateway = dockerGateway;

            Id = _dockerGateway.ContainerRun(attributes.OfType<Image>().Single().Name, attributes.OfType<Command>().SingleOrDefault()?.Value ?? string.Empty);
            IpAddress = _dockerGateway.ContainerGetDefaultNetworkIpAddress(Id);
        }

        public void Dispose()
        {
            _dockerGateway.ContainerRemove(Id);
        }
    }
}
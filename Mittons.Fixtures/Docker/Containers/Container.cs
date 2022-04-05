using System;
using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Containers
{
    public class Container : IDisposable
    {
        public string Id { get; }

        private readonly IDockerGateway _dockerGateway;

        public Container(IDockerGateway dockerGateway, IEnumerable<Attribute> attributes)
        {
            _dockerGateway = dockerGateway;

            Id = _dockerGateway.Run(attributes.OfType<Image>().Single().Name, attributes.OfType<Command>().SingleOrDefault()?.Value ?? string.Empty);
        }

        public void Dispose()
        {
            _dockerGateway.Remove(Id);
        }
    }
}
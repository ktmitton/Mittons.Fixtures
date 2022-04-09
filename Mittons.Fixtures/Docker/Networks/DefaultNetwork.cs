using System;
using System.Collections.Generic;
using System.Threading;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Networks
{
    public class DefaultNetwork : IDisposable
    {
        private readonly IDockerGateway _dockerGateway;

        private readonly string _name;

        public DefaultNetwork(IDockerGateway dockerGateway, string name, Dictionary<string, string> labels)
        {
            _dockerGateway = dockerGateway;
            _name = name;

            _dockerGateway.NetworkCreateAsync(_name, labels, CancellationToken.None).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _dockerGateway.NetworkRemoveAsync(_name, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Gateways;
using Xunit;

namespace Mittons.Fixtures.Docker.Networks
{
    public class DefaultNetwork : IAsyncLifetime
    {
        private readonly IDockerGateway _dockerGateway;

        private readonly string _name;

        private readonly Dictionary<string, string> _labels;

        public DefaultNetwork(IDockerGateway dockerGateway, string name, Dictionary<string, string> labels)
        {
            _dockerGateway = dockerGateway;
            _name = name;
            _labels = labels;
        }

        public Task DisposeAsync()
            => _dockerGateway.NetworkRemoveAsync(_name, CancellationToken.None);

        public Task InitializeAsync()
            => _dockerGateway.NetworkCreateAsync(_name, _labels, CancellationToken.None);
    }
}
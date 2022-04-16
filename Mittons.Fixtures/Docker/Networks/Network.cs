using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Networks
{
    public class Network : IAsyncLifetime
    {
        private readonly INetworkGateway _networkGateway;

        private readonly string _name;

        private readonly IEnumerable<Option> _options;

        private readonly bool _teardownOnComplete;

        public Network(INetworkGateway networkGateway, string name, IEnumerable<Attribute> attributes)
        {
            _networkGateway = networkGateway;
            _name = name;

            var run = attributes.OfType<RunAttribute>().Single();

            _options = run.Options;
            _teardownOnComplete = run.TeardownOnComplete;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked when an instance of <see cref="Network"/> is no longer used.
        /// </remarks>
        public Task DisposeAsync()
            => _teardownOnComplete ? _networkGateway.RemoveAsync(_name, CancellationToken.None) : Task.CompletedTask;

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked after an instance of <see cref="Network"/> is created, before it is used.
        /// </remarks>
        public Task InitializeAsync(CancellationToken cancellationToken)
            => _networkGateway.CreateAsync(_name, _options, cancellationToken);
    }
}
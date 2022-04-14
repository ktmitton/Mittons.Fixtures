using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Networks
{
    public class DefaultNetwork : IAsyncLifetime
    {
        private readonly IDockerGateway _dockerGateway;

        private readonly string _name;

        private readonly IEnumerable<KeyValuePair<string, string>> _options;

        private readonly bool _teardownOnComplete;

        public DefaultNetwork(IDockerGateway dockerGateway, string name, IEnumerable<Attribute> attributes)
        {
            _dockerGateway = dockerGateway;
            _name = name;

            var run = attributes.OfType<RunAttribute>().Single();

            _options = run.Options;
            _teardownOnComplete = run.TeardownOnComplete;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked when an instance of <see cref="DefaultNetwork"/> is no longer used.
        /// </remarks>
        public Task DisposeAsync()
            => _teardownOnComplete ? _dockerGateway.NetworkRemoveAsync(_name, CancellationToken.None) : Task.CompletedTask;

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked after an instance of <see cref="DefaultNetwork"/> is created, before it is used.
        /// </remarks>
        public Task InitializeAsync(CancellationToken cancellationToken)
            => _dockerGateway.NetworkCreateAsync(_name, _options, cancellationToken);
    }
}
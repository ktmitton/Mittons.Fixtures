using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Networks
{
    public class DefaultNetwork : IAsyncLifetime
    {
        private readonly IDockerGateway _dockerGateway;

        private readonly string _name;

        private readonly IEnumerable<KeyValuePair<string, string>> _options;

        public DefaultNetwork(IDockerGateway dockerGateway, string name, IEnumerable<KeyValuePair<string, string>> options)
        {
            _dockerGateway = dockerGateway;
            _name = name;
            _options = options;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked when an instance of <see cref="DefaultNetwork"/> is no longer used.
        /// </remarks>
        public Task DisposeAsync()
            => _dockerGateway.NetworkRemoveAsync(_name, CancellationToken.None);

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked after an instance of <see cref="DefaultNetwork"/> is created, before it is used.
        /// </remarks>
        public Task InitializeAsync()
            => _dockerGateway.NetworkCreateAsync(_name, _options, CancellationToken.None);
    }
}
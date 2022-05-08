using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Services
{
    public class ContainerNetworkService : IContainerNetworkService
    {
        public string Name { get; private set; }

        public string ServiceId { get; private set; }

        public IEnumerable<IResource> Resources { get; private set; }

        public IEnumerable<IResourceAdapter> ResourceAdapters { get; private set; }

        private readonly IContainerNetworkGateway _containerNetworkGateway;

        private bool _teardownOnDispose;

        public ContainerNetworkService(IContainerNetworkGateway containerNetworkGateway)
        {
            _containerNetworkGateway = containerNetworkGateway;
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(_teardownOnDispose ? _containerNetworkGateway.RemoveNetworkAsync(default, default) : Task.CompletedTask);
        }

        public Task ConnectAsync(NetworkAliasAttribute networkAlias, CancellationToken cancellationToken)
        {
            if (!(networkAlias.ConnectedService is IContainerService))
            {
                throw new NotSupportedException($"ContainerNetworkService does not support connecting {networkAlias.ConnectedService.GetType()} services.");
            }

            return _containerNetworkGateway.ConnectAsync(ServiceId, networkAlias.ConnectedService.ServiceId, networkAlias.Alias, cancellationToken);
        }

        /// <exception cref="System.InvalidOperationException">Thrown when <paramref name="attributes"/> does not contain exactly one instance of <see cref="Mittons.Fixtures.Attributes.RunAttribure"/>.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when <paramref name="attributes"/> does not contain exactly one instance of <see cref="Mittons.Fixtures.Attributes.NetworkAttribute"/>.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the <see cref="Mittons.Fixtures.Attributes.NetworkAttribute"/> in <paramref name="attributes"/> contains an invalid network name.</exception>
        public async Task InitializeAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
        {
            var run = attributes.OfType<RunAttribute>().Single();

            _teardownOnDispose = run.TeardownOnComplete;

            var network = attributes.OfType<ServiceAttribute>().Single();

            if (string.IsNullOrWhiteSpace(network.Name))
            {
                throw new ArgumentException("Name cannot be null or whitespace.", "attributes{ServiceAttribute.Name}");
            }

            Name = network.Name;

            ServiceId = await _containerNetworkGateway.CreateNetworkAsync(
                    Name,
                    new Dictionary<string, string>
                    {
                        { "mittons.fixtures.run.id", run.Id }
                    },
                    cancellationToken
                ).ConfigureAwait(false);

            Resources = Enumerable.Empty<IResource>();

            ResourceAdapters = Enumerable.Empty<IResourceAdapter>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Docker.Networks;

namespace Mittons.Fixtures.Docker.Fixtures
{
    public abstract class DockerEnvironmentFixture : IAsyncLifetime
    {
        public Guid InstanceId { get; } = Guid.NewGuid();

        private readonly List<Container> _containers;

        private readonly Network[] _networks;

        public DockerEnvironmentFixture()
            : this(new ContainerGateway(), new NetworkGateway())
        {
        }

        public DockerEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
        {
            var environmentAttributes = Attribute.GetCustomAttributes(this.GetType());

            var run = environmentAttributes.OfType<RunAttribute>().SingleOrDefault();

            if (run is null)
            {
                run = new RunAttribute();
                environmentAttributes = environmentAttributes.Concat(new[] { run }).ToArray();
            }

            var networks = environmentAttributes.OfType<NetworkAttribute>();
            var duplicateNetworks = networks.GroupBy(x => x.Name).Where(x => x.Count() > 1);

            if (duplicateNetworks.Any())
            {
                throw new NotSupportedException($"Networks with the same name cannot be created for the same environment. The following networks were duplicated: [{string.Join(", ", duplicateNetworks.Select(x => x.Key))}]");
            }

            _networks = networks.Select(x => new Network(networkGateway, $"{x.Name}-{InstanceId}", environmentAttributes)).ToArray();

            _containers = new List<Container>();

            foreach (var propertyInfo in this.GetType().GetProperties().Where(x => typeof(Container).IsAssignableFrom(x.PropertyType)))
            {
                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                var container = (Container)Activator.CreateInstance(propertyInfo.PropertyType, new object[] { containerGateway, networkGateway, InstanceId, attributes });
                propertyInfo.SetValue(this, container);
                _containers.Add(container);
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked after an instance of <see cref="DockerEnvironmentFixture"/> is created, before it is used.
        /// </remarks>
        public Task InitializeAsync()
            => InitializeAsync(new CancellationToken());

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked after an instance of <see cref="DockerEnvironmentFixture"/> is created, before it is used.
        /// </remarks>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(_networks.Select(x => x.InitializeAsync(cancellationToken))).ConfigureAwait(false);

            await Task.WhenAll(_containers.Select(x => x.InitializeAsync(cancellationToken))).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked when an instance of <see cref="DockerEnvironmentFixture"/> is no longer used.
        /// </remarks>
        public async Task DisposeAsync()
        {
            await Task.WhenAll(_containers.Select(x => x.DisposeAsync())).ConfigureAwait(false);

            await Task.WhenAll(_networks.Select(x => x.DisposeAsync())).ConfigureAwait(false);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Attributes;

namespace Mittons.Fixtures
{
    /// <summary>
    /// An abstract base class from which Guest environments will be defined for testing.
    /// </summary>
    /// <remarks>
    /// Guest environments will extend this class and include properties that define the various services and networks that make up the environment.
    /// </remarks>
    public abstract class GuestEnvironmentFixture
    {
        /// <summary>
        /// Gets a unique identifier for the instance of this environment.
        /// </summary>
        /// <remarks>
        /// This id is typically attributed to the various services or networks created by the environment to aid in cleanup if the testing is cancelled before proper disposal.
        /// </remarks>
        /// <returns>
        /// A unique identifier to the current instance.
        /// </returns>
        /// <value>
        /// An identifier the Host environment can use to find services or networks that weren't properly disposed so it can clean them up.
        /// </value>
        public Guid InstanceId { get; } = Guid.NewGuid();

        private readonly List<IService> _services;

        private readonly List<INetwork> _networks;

        private readonly Attribute[] _environmentAttributes;

        private readonly IServiceGatewayFactory _serviceGatewayFactory;

        private readonly INetworkGatewayFactory _networkGatewayFactory;

        /// <summary>
        /// A constructor for a new instance of the Guest environment with custom parameters.
        /// </summary>
        /// <param name="serviceGatewayFactory">
        /// The <see cref="Mittons.Fixtures.IServiceGatewayFactory"/> that should be used to create instances of <see cref="Mittons.Fixtures.IServiceGateway{TService}"/> as needed.
        /// </param>
        /// <remarks>
        /// This allows consumers to provide their own implementation of <see cref="Mittons.Fixtures.IServiceGatewayFactory"/> if the default factory does not provide the desired functionality.
        /// </remarks>
        public GuestEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory = null, INetworkGatewayFactory networkGatewayFactory = null)
        {
            _serviceGatewayFactory = serviceGatewayFactory ?? new DefaultServiceGatewayFactory();

            _networkGatewayFactory = networkGatewayFactory ?? new DefaultNetworkGatewayFactory();

            var environmentAttributes = Attribute.GetCustomAttributes(this.GetType());

            _environmentAttributes = environmentAttributes.OfType<RunAttribute>().Any() ? environmentAttributes : environmentAttributes.Concat(new[] { new RunAttribute() }).ToArray();

            _services = new List<IService>();

            _networks = new List<INetwork>();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked after an instance of <see cref="DockerEnvironmentFixture"/> is created, before it is used.
        /// </remarks>
        public virtual Task InitializeAsync()
            => InitializeAsync(new CancellationToken());

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked after an instance of <see cref="DockerEnvironmentFixture"/> is created, before it is used.
        /// </remarks>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            var run = _environmentAttributes.OfType<RunAttribute>().Single();

            foreach (var propertyInfo in this.GetType().GetProperties().Where(x => typeof(INetwork).IsAssignableFrom(x.PropertyType)))
            {
                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                var networkGateway = _networkGatewayFactory.GetNetworkGateway(propertyInfo.PropertyType);

                var network = await networkGateway.CreateNetworkAsync(attributes, cancellationToken).ConfigureAwait(false);

                propertyInfo.SetValue(this, network);
                _networks.Add(network);
            }

            foreach (var propertyInfo in this.GetType().GetProperties().Where(x => typeof(IService).IsAssignableFrom(x.PropertyType)))
            {
                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                var serviceGateway = _serviceGatewayFactory.GetServiceGateway(propertyInfo.PropertyType);

                var service = await serviceGateway.CreateServiceAsync(attributes, cancellationToken).ConfigureAwait(false);

                propertyInfo.SetValue(this, service);
                _services.Add(service);

                var networkAliases = attributes.OfType<NetworkAliasAttribute>();

                if (networkAliases.Any())
                {
                    foreach (var alias in networkAliases)
                    {
                        var network = _networks.Single(x => x.Name == alias.NetworkName);

                        var networkGateway = _networkGatewayFactory.GetNetworkGateway(network.GetType());

                        await networkGateway.ConnectServiceAsync(network, service, alias, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked when an instance of <see cref="DockerEnvironmentFixture"/> is no longer used.
        /// </remarks>
        public async Task DisposeAsync()
        {
            if (_environmentAttributes.OfType<RunAttribute>().Single().TeardownOnComplete)
            {
                await Task.WhenAll(_services.Select(x => _serviceGatewayFactory.GetServiceGateway(x.GetType()).RemoveServiceAsync(x, CancellationToken.None))).ConfigureAwait(false);
                await Task.WhenAll(_networks.Select(x => _networkGatewayFactory.GetNetworkGateway(x.GetType()).RemoveNetworkAsync(x, CancellationToken.None))).ConfigureAwait(false);
            }
        }

        private class DefaultServiceGatewayFactory : IServiceGatewayFactory
        {
            public IServiceGateway<IService> GetServiceGateway(Type serviceType)
            {
                var serviceGatewayType = typeof(GuestEnvironmentFixture).Assembly.GetTypes().First(x => x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IServiceGateway<>) && i.GenericTypeArguments.Any(z => z == serviceType)));

                var baseGateway = Activator.CreateInstance(serviceGatewayType);

                var decoratorGateway = typeof(ServiceGatewayDecorator<>).MakeGenericType(serviceType);

                return (IServiceGateway<IService>)Activator.CreateInstance(decoratorGateway, new object[] { baseGateway });
            }

            private class ServiceGatewayDecorator<T> : IServiceGateway<IService> where T : IService
            {
                private readonly IServiceGateway<T> _baseGateway;

                public ServiceGatewayDecorator(IServiceGateway<T> baseGateway)
                {
                    _baseGateway = baseGateway;
                }

                public async Task<IService> CreateServiceAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
                    => await _baseGateway.CreateServiceAsync(attributes, cancellationToken).ConfigureAwait(false);

                public async Task RemoveServiceAsync(IService service, CancellationToken cancellationToken)
                    => await _baseGateway.RemoveServiceAsync((T)service, cancellationToken).ConfigureAwait(false);
            }
        }

        private class DefaultNetworkGatewayFactory : INetworkGatewayFactory
        {
            public INetworkGateway<INetwork> GetNetworkGateway(Type networkType)
            {
                var networkGatewayType = typeof(GuestEnvironmentFixture).Assembly.GetTypes().First(x => x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INetworkGateway<>) && i.GenericTypeArguments.Any(z => z == networkType)));

                var baseGateway = Activator.CreateInstance(networkGatewayType);

                var decoratorGateway = typeof(NetworkGatewayDecorator<>).MakeGenericType(networkType);

                return (INetworkGateway<INetwork>)Activator.CreateInstance(decoratorGateway, new object[] { baseGateway });
            }

            private class NetworkGatewayDecorator<T> : INetworkGateway<INetwork> where T : INetwork
            {
                private readonly INetworkGateway<T> _baseGateway;

                public NetworkGatewayDecorator(INetworkGateway<T> baseGateway)
                {
                    _baseGateway = baseGateway;
                }

                public async Task<INetwork> CreateNetworkAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
                    => await _baseGateway.CreateNetworkAsync(attributes, cancellationToken).ConfigureAwait(false);

                public async Task RemoveNetworkAsync(INetwork network, CancellationToken cancellationToken)
                    => await _baseGateway.RemoveNetworkAsync((T)network, cancellationToken).ConfigureAwait(false);

                public Task ConnectServiceAsync(INetwork network, IService service, NetworkAliasAttribute alias, CancellationToken cancellationToken)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}

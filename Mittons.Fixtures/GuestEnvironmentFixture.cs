using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mittons.Fixtures.Attributes;
using Mittons.Fixtures.Containers;

namespace Mittons.Fixtures
{
    /// <summary>
    /// An abstract base class from which Guest environments will be defined for testing.
    /// </summary>
    /// <remarks>
    /// Guest environments will extend this class and include properties that define the various services and networks that make up the environment.
    /// </remarks>
    public abstract class GuestEnvironmentFixture : IAsyncDisposable
    {
        // /// <summary>
        // /// Gets a unique identifier for the instance of this environment.
        // /// </summary>
        // /// <remarks>
        // /// This id is typically attributed to the various services or networks created by the environment to aid in cleanup if the testing is cancelled before proper disposal.
        // /// </remarks>
        // /// <returns>
        // /// A unique identifier to the current instance.
        // /// </returns>
        // /// <value>
        // /// An identifier the Host environment can use to find services or networks that weren't properly disposed so it can clean them up.
        // /// </value>
        // public Guid InstanceId { get; } = Guid.NewGuid();

        // private readonly List<IService> _services;

        // private readonly List<INetwork> _networks;

        // private readonly Attribute[] _environmentAttributes;

        // private readonly IServiceGatewayFactory _serviceGatewayFactory;

        // private readonly INetworkGatewayFactory _networkGatewayFactory;

        protected readonly ServiceCollection _serviceCollection;

        private ServiceProvider _serviceProvider;

        // private readonly IServiceProvider _serviceProvider;

        public GuestEnvironmentFixture()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddSingleton<IContainerNetworkService, ContainerNetworkService>();
            //_serviceCollection.AddSingleton<INetworkGateway<IContainerNetwork>, DockerNetworkGateway>();
        }

        public ValueTask DisposeAsync()
        {
            return _serviceProvider?.DisposeAsync() ?? new ValueTask(Task.CompletedTask);
        }

        // /// <summary>
        // /// A constructor for a new instance of the Guest environment with custom parameters.
        // /// </summary>
        // /// <param name="serviceGatewayFactory">
        // /// The <see cref="Mittons.Fixtures.IServiceGatewayFactory"/> that should be used to create instances of <see cref="Mittons.Fixtures.IServiceGateway{TService}"/> as needed.
        // /// </param>
        // /// <remarks>
        // /// This allows consumers to provide their own implementation of <see cref="Mittons.Fixtures.IServiceGatewayFactory"/> if the default factory does not provide the desired functionality.
        // /// </remarks>
        // public GuestEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory = null, INetworkGatewayFactory networkGatewayFactory = null)
        // {
        //     _serviceCollection = new ServiceCollection();
        //     _serviceCollection.Clear();
        //     //_serviceCollection.AddSingleton<INetworkGateway<IContainerNetwork>, DockerNetworkGateway>();
        //     _serviceProvider = _serviceCollection.BuildServiceProvider();

        //     _serviceGatewayFactory = serviceGatewayFactory ?? new DefaultServiceGatewayFactory();

        //     _networkGatewayFactory = networkGatewayFactory ?? new DefaultNetworkGatewayFactory();

        //     var environmentAttributes = Attribute.GetCustomAttributes(this.GetType());

        //     _environmentAttributes = environmentAttributes.OfType<RunAttribute>().Any() ? environmentAttributes : environmentAttributes.Concat(new[] { new RunAttribute() }).ToArray();

        //     _services = new List<IService>();

        //     _networks = new List<INetwork>();
        // }

        /// <inheritdoc/>
        /// <exception cref="System.InvalidOperationException">Thrown when an environment property cannot be resolved.</exception>
        /// <remarks>
        /// This must be invoked after an instance of <see cref="DockerEnvironmentFixture"/> is created, before it is used.
        /// </remarks>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var environmentAttributes = Attribute.GetCustomAttributes(this.GetType());

            var run = environmentAttributes.OfType<RunAttribute>().FirstOrDefault() ?? new RunAttribute();

            _serviceProvider = _serviceCollection.BuildServiceProvider();

            foreach (var propertyInfo in this.GetType().GetProperties().Where(x => typeof(INetworkService).IsAssignableFrom(x.PropertyType)))
            {
                var network = _serviceProvider.GetRequiredService(propertyInfo.PropertyType);

                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                propertyInfo.SetValue(this, network);

                await ((IService)network).InitializeAsync(attributes, cancellationToken);
            }

            // foreach (var propertyInfo in this.GetType().GetProperties().Where(x => typeof(IService).IsAssignableFrom(x.PropertyType)))
            // {
            //     var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

            //     var serviceGateway = _serviceGatewayFactory.GetServiceGateway(propertyInfo.PropertyType);

            //     var service = await serviceGateway.CreateServiceAsync(attributes, cancellationToken).ConfigureAwait(false);

            //     propertyInfo.SetValue(this, service);
            //     _services.Add(service);

            //     var networkAliases = attributes.OfType<NetworkAliasAttribute>();

            //     if (networkAliases.Any())
            //     {
            //         foreach (var alias in networkAliases)
            //         {
            //             var network = _networks.Single(x => x.Name == alias.NetworkName);

            //             var networkGateway = _networkGatewayFactory.GetNetworkGateway(network.GetType());

            //             await networkGateway.ConnectServiceAsync(network, service, alias, cancellationToken).ConfigureAwait(false);
            //         }
            //     }
            // }
        }
    }
}

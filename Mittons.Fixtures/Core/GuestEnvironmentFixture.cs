using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Containers.Gateways.Docker;
using Mittons.Fixtures.Containers.Services;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Services;

namespace Mittons.Fixtures.Core
{
    /// <summary>
    /// An abstract base class from which Guest environments will be defined for testing.
    /// </summary>
    /// <remarks>
    /// Guest environments will extend this class and include properties that define the various services and networks that make up the environment.
    /// </remarks>
    public abstract class GuestEnvironmentFixture : IAsyncDisposable
    {
        protected readonly ServiceCollection _serviceCollection;

        private ServiceProvider _serviceProvider;

        protected GuestEnvironmentFixture()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddTransient<IContainerNetworkService, ContainerNetworkService>();
            _serviceCollection.AddTransient<IContainerService, ContainerService>();
            _serviceCollection.AddSingleton<IContainerGateway, ContainerGateway>();
            _serviceCollection.AddSingleton<IContainerNetworkGateway, ContainerNetworkGateway>();
        }

        public ValueTask DisposeAsync()
        {
            return _serviceProvider?.DisposeAsync() ?? new ValueTask(Task.CompletedTask);
        }

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

            var services = this.GetType().GetProperties().Where(x => typeof(IService).IsAssignableFrom(x.PropertyType));

            var networks = new Dictionary<string, INetworkService>();

            foreach (var propertyInfo in services.Where(x => typeof(INetworkService).IsAssignableFrom(x.PropertyType)))
            {
                var network = (INetworkService)_serviceProvider.GetRequiredService(propertyInfo.PropertyType);

                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                await network.InitializeAsync(attributes, cancellationToken).ConfigureAwait(false);

                propertyInfo.SetValue(this, network);

                networks.Add(network.Name, network);
            }

            var servicePropertyInfos = services.Where(x => !typeof(INetworkService).IsAssignableFrom(x.PropertyType)).ToArray();
            var dependencyGraph = new DependencyGraph<PropertyInfo>(servicePropertyInfos, x => x.Name, x => x.GetCustomAttributes(false).OfType<ServiceDependencyAttribute>().Select(y => y.Name));

            foreach (var propertyInfo in dependencyGraph.CreateBuildOrder())
            {
                var service = (IService)_serviceProvider.GetRequiredService(propertyInfo.PropertyType);

                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                foreach (var attribute in attributes.OfType<NetworkAliasAttribute>())
                {
                    attribute.NetworkService = networks[attribute.NetworkName];
                    attribute.ConnectedService = service;
                }

                propertyInfo.SetValue(this, service);

                await service.InitializeAsync(attributes, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

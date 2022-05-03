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

        public GuestEnvironmentFixture()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddSingleton<IContainerNetworkService, ContainerNetworkService>();
            _serviceCollection.AddSingleton<IContainerService, ContainerService>();
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

        private Task<IService> CreateAndInitializeServiceAsync(PropertyInfo propertyInfo)
        {
            return Task.FromResult(default(IService));
        }
    }

    // public class DependencyGraph
    // {
    //     public class Node
    //     {
    //         public string Name { get; set; }

    //         public List<Node> Depdencies { get; set; }
    //     }

    //     private readonly Dictionary<string, Node> _nodes;

    //     public DependencyGraph(IEnumerable<KeyValuePair<string, string>> links)
    //     {
    //         foreach (var link in links)
    //         {
    //             if (!_nodes.ContainsKey(link.Value))
    //             {
    //                 _nodes[link.Value] = new Node { Name = link.Value, Depdencies = new List<Node>() };
    //             }

    //             if (!_nodes.ContainsKey(link.Key))
    //             {
    //                 _nodes[link.Key] = new Node { Name = link.Key, Depdencies = new List<Node>() };
    //             }

    //             if (!_nodes[link.Key].Depdencies.Any(x => x.Name == link.Value))
    //             {
    //                 _nodes[link.Key].Depdencies.Add(_nodes[link.Value]);
    //             }
    //         }
    //         _nodes = new Dictionary<string, Node>();
    //     }
    // }
}

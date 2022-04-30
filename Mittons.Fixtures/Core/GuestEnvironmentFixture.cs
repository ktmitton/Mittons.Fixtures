using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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

            foreach (var propertyInfo in services.Where(x => typeof(INetworkService).IsAssignableFrom(x.PropertyType)))
            {
                var network = _serviceProvider.GetRequiredService(propertyInfo.PropertyType);

                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                propertyInfo.SetValue(this, network);

                await ((IService)network).InitializeAsync(attributes, cancellationToken);
            }

            foreach (var propertyInfo in services.Where(x => !typeof(INetworkService).IsAssignableFrom(x.PropertyType)))
            {
                var service = _serviceProvider.GetRequiredService(propertyInfo.PropertyType);

                var attributes = new[] { run };

                propertyInfo.SetValue(this, service);

                await ((IService)service).InitializeAsync(attributes, cancellationToken);
            }
        }
    }
}

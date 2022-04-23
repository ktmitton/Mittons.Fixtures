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

        private readonly Attribute[] _environmentAttributes;

        private readonly IServiceGatewayFactory _serviceGatewayFactory;

        /// <summary>
        /// The default constructor for a new instance of the Guest environment.
        /// </summary>
        /// <remarks>
        /// This will create the Guest environment using the default <see cref="Mittons.Fixtures.IServiceGatewayFactory"/>.
        /// </remarks>
        public GuestEnvironmentFixture()
            : this(new DefaultServiceGatewayFactory())
        {
        }

        /// <summary>
        /// A constructor for a new instance of the Guest environment with custom parameters.
        /// </summary>
        /// <param name="serviceGatewayFactory">
        /// The <see cref="Mittons.Fixtures.IServiceGatewayFactory"/> that should be used to create instances of <see cref="Mittons.Fixtures.IServiceGateway{TService}"/> as needed.
        /// </param>
        /// <remarks>
        /// This allows consumers to provide their own implementation of <see cref="Mittons.Fixtures.IServiceGatewayFactory"/> if the default factory does not provide the desired functionality.
        /// </remarks>
        public GuestEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory)
        {
            _serviceGatewayFactory = serviceGatewayFactory;

            var environmentAttributes = Attribute.GetCustomAttributes(this.GetType());

            _environmentAttributes = environmentAttributes.OfType<RunAttribute>().Any() ? environmentAttributes : environmentAttributes.Concat(new[] { new RunAttribute() }).ToArray();

            _services = new List<IService>();
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

            foreach (var propertyInfo in this.GetType().GetProperties().Where(x => typeof(IService).IsAssignableFrom(x.PropertyType)))
            {
                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                var serviceGateway = _serviceGatewayFactory.GetServiceGateway(propertyInfo.PropertyType);

                var service = await serviceGateway.CreateServiceAsync(attributes, cancellationToken).ConfigureAwait(false);

                propertyInfo.SetValue(this, service);
                _services.Add(service);
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
    }
}
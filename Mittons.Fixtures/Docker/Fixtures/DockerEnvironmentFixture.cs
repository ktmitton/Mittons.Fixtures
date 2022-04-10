using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Docker.Networks;
using Xunit;

namespace Mittons.Fixtures.Docker.Fixtures
{
    public abstract class DockerEnvironmentFixture : IAsyncLifetime
    {
        public Guid InstanceId { get; } = Guid.NewGuid();

        private readonly List<Container> _containers;

        private readonly DefaultNetwork[] _networks;

        public DockerEnvironmentFixture()
            : this(new DefaultDockerGateway())
        {
        }

        public DockerEnvironmentFixture(IDockerGateway dockerGateway)
        {
            var environmentAttributes = Attribute.GetCustomAttributes(this.GetType());

            var run = environmentAttributes.OfType<Run>().SingleOrDefault() ?? new Run();

            var networks = environmentAttributes.OfType<Network>();
            var duplicateNetworks = networks.GroupBy(x => x.Name).Where(x => x.Count() > 1);

            if (duplicateNetworks.Any())
            {
                throw new NotSupportedException($"Networks with the same name cannot be created for the same environment. The following networks were duplicated: [{string.Join(", ", duplicateNetworks.Select(x => x.Key))}]");
            }

            _networks = networks.Select(x => new DefaultNetwork(dockerGateway, $"{x.Name}-{InstanceId}", run.Options)).ToArray();

            _containers = new List<Container>();

            foreach (var propertyInfo in this.GetType().GetProperties().Where(x => typeof(Container).IsAssignableFrom(x.PropertyType)))
            {
                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                var container = (Container)Activator.CreateInstance(propertyInfo.PropertyType, new object[] { dockerGateway, InstanceId, attributes });
                propertyInfo.SetValue(this, container);
                _containers.Add(container);
            }

            var addRangeMethod = _containers.GetType().GetMethod("AddRange");

            foreach (var propertyInfo in this.GetType().GetProperties().Where(x => typeof(IEnumerable<Container>).IsAssignableFrom(x.PropertyType)))
            {
                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>().Concat(new[] { run });

                var scale = attributes.OfType<Scale>().FirstOrDefault()?.Count ?? 1;

                var containerCollection = Activator.CreateInstance(typeof(List<>).MakeGenericType(propertyInfo.PropertyType.GenericTypeArguments[0]));

                var addMethod = containerCollection.GetType().GetMethod("Add");

                for (var i = 0; i < scale; ++i)
                {
                    var container = Activator.CreateInstance(propertyInfo.PropertyType.GenericTypeArguments[0], new object[] { dockerGateway, InstanceId, attributes });

                    addMethod.Invoke(containerCollection, new object[] { container });
                }

                propertyInfo.GetSetMethod().Invoke(this, new object[] { containerCollection });

                addRangeMethod.Invoke(_containers, new object[] { containerCollection });
            }
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(_networks.Select(x => x.InitializeAsync()));

            await Task.WhenAll(_containers.Select(x => x.InitializeAsync()));
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(_containers.Select(x => x.DisposeAsync()));

            await Task.WhenAll(_networks.Select(x => x.DisposeAsync()));
        }
    }
}
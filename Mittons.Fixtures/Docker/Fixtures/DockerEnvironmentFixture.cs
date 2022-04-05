using System;
using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Fixtures
{
    public abstract class DockerEnvironmentFixture
    {
        private List<Container> _containers;

        public DockerEnvironmentFixture(IDockerGateway dockerGateway)
        {
            _containers = new List<Container>();

            foreach(var propertyInfo in this.GetType().GetProperties().Where(x => x.PropertyType.IsEquivalentTo(typeof(Container))))
            {
                var container = (Container)Activator.CreateInstance(propertyInfo.PropertyType, new object[] { dockerGateway, propertyInfo.GetCustomAttributes(false).OfType<Image>().Single(), string.Empty});
                propertyInfo.SetValue(this, container);
                _containers.Add(container);
            }

            foreach(var propertyInfo in this.GetType().GetProperties().Where(x => x.PropertyType.IsEquivalentTo(typeof(SftpContainer))))
            {
                var container = (Container)Activator.CreateInstance(propertyInfo.PropertyType, new object[] { dockerGateway, propertyInfo.GetCustomAttributes(false).OfType<SftpUserAccount>()});
                propertyInfo.SetValue(this, container);
                _containers.Add(container);
            }
        }

        public void Dispose()
        {
            foreach(var container in _containers)
            {
                container.Dispose();
            }
        }
    }
}
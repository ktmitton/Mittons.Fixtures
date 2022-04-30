using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Containers
{
    internal class ContainerNetworkService : IContainerNetworkService
    {
        public string NetworkId { get; }

        public string Name { get; }

        public string ServiceId => throw new System.NotImplementedException();

        public IEnumerable<IResource> Resources => throw new System.NotImplementedException();

        public ContainerNetworkService()
        {
        }

        public ContainerNetworkService(string networkId, string name)
        {
            NetworkId = networkId;
            Name = name;
        }

        public Task InitializeAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
    }
}

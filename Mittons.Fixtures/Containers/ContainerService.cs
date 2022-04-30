using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Containers
{
    internal class ContainerService : IContainerService
    {
        public IEnumerable<IResource> Resources { get; private set; }

        public string ServiceId { get; }

        private Func<IContainerService, Task> _disposeCallback;

        public ContainerService()
        {
        }

        public ContainerService(string serviceId, IEnumerable<IResource> resources, Func<IContainerService, Task> disposeCallback)
        {
            ServiceId = serviceId;
            Resources = resources;

            _disposeCallback = disposeCallback;
        }

        public async ValueTask DisposeAsync()
        {
            await _disposeCallback(this);
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InitializeAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

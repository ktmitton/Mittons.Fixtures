using System.Collections.Generic;

namespace Mittons.Fixtures.Containers
{
    internal class ContainerService : IContainerService
    {
        public IEnumerable<IResource> Resources { get; private set; }

        public string ContainerId { get; }

        public ContainerService(string containerId, IEnumerable<IResource> resources)
        {
            ContainerId = containerId;
            Resources = resources;
        }
    }
}

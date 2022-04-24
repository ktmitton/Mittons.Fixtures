using System.Collections.Generic;

namespace Mittons.Fixtures.Containers
{
    internal class ContainerService : IContainerService
    {
        public IEnumerable<IResource> Resources { get; private set; }

        public string ServiceId { get; }

        public ContainerService(string serviceId, IEnumerable<IResource> resources)
        {
            ServiceId = serviceId;
            Resources = resources;
        }
    }
}

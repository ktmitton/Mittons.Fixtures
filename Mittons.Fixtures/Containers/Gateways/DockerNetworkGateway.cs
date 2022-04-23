using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Containers.Gateways
{
    public class DockerNetworkGateway : INetworkGateway<IContainerNetwork>
    {
        /// <inheritdoc/>
        public Task<IContainerNetwork> CreateNetworkAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveNetworkAsync(INetwork network, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

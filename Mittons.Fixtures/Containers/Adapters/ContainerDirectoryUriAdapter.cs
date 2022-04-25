using System;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Adapters;

namespace Mittons.Fixtures.Containers.Adapters
{
    internal class ContainerDirectoryUriAdapter : IDirectoryUriAdapter
    {
        public bool CanSupportUri(Uri uri)
        {
            throw new NotImplementedException();
        }

        public Task EmptyAsync(CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }
}

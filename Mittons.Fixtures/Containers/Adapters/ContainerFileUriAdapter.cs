using System;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Adapters;

namespace Mittons.Fixtures.Containers.Adapters
{
    internal class ContainerFileUriAdapter : IFileUriAdapter
    {
        public bool CanSupportUri(Uri uri)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetOwnerAsync(string owner, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetPermissionsAsync(string permissions, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(string contents, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

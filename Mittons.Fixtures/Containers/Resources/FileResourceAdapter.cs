using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Resources
{
    internal class FileResourceAdapter : IFileResourceAdapter
    {
        private readonly string _containerId;

        private readonly string _path;

        private readonly IContainerGateway _containerGateway;

        public FileResourceAdapter(IResource resource, IContainerGateway containerGateway)
        {
            _containerId = resource.GuestUri.Host.Split(new[] { '.' }, 2)[1];
            _path = resource.GuestUri.AbsolutePath;
            _containerGateway = containerGateway;
        }

        public Task AppendAsync(string contents, CancellationToken cancellationToken)
            => _containerGateway.AppendFileAsync(_containerId, _path, contents, cancellationToken);

        public Task CreateAsync(CancellationToken cancellationToken)
            => _containerGateway.CreateFileAsync(_containerId, _path, cancellationToken);

        public Task DeleteAsync(CancellationToken cancellationToken)
            => _containerGateway.DeleteFileAsync(_containerId, _path, cancellationToken);

        public Task<string> ReadAsync(CancellationToken cancellationToken)
            => _containerGateway.ReadFileAsync(_containerId, _path, cancellationToken);

        public Task SetOwnerAsync(string owner, CancellationToken cancellationToken)
            => _containerGateway.SetFileSystemResourceOwnerAsync(_containerId, _path, owner, cancellationToken);

        public Task SetPermissionsAsync(string permissions, CancellationToken cancellationToken)
            => _containerGateway.SetFileSystemResourcePermissionsAsync(_containerId, _path, permissions, cancellationToken);

        public Task WriteAsync(string contents, CancellationToken cancellationToken)
            => _containerGateway.WriteFileAsync(_containerId, _path, contents, cancellationToken);
    }
}

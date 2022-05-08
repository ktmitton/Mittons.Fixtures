using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Resources
{
    internal class DirectoryResourceAdapter : IDirectoryResourceAdapter
    {
        private readonly string _containerId;

        public string Path { get; }

        private readonly IContainerGateway _containerGateway;

        public DirectoryResourceAdapter(IResource resource, IContainerGateway containerGateway)
        {
            _containerId = resource.GuestUri.Host.Split(new[] { '.' }, 2)[1];
            Path = resource.GuestUri.AbsolutePath;
            _containerGateway = containerGateway;
        }

        public DirectoryResourceAdapter(string containerId, string path, IContainerGateway containerGateway)
        {
            _containerId = containerId;
            Path = path;
            _containerGateway = containerGateway;
        }

        public Task CreateAsync(CancellationToken cancellationToken)
            => _containerGateway.CreateDirectoryAsync(_containerId, Path, cancellationToken);

        public Task DeleteAsync(bool recursive, CancellationToken cancellationToken)
            => _containerGateway.DeleteDirectoryAsync(_containerId, Path, recursive, cancellationToken);

        public Task<IEnumerable<IDirectoryResourceAdapter>> EnumerateDirectoriesAsync(CancellationToken cancellationToken)
            => _containerGateway.EnumerateDirectoriesAsync(_containerId, Path, cancellationToken);

        public Task<IEnumerable<IFileResourceAdapter>> EnumerateFilesAsync(CancellationToken cancellationToken)
            => _containerGateway.EnumerateFilesAsync(_containerId, Path, cancellationToken);

        public Task SetOwnerAsync(string owner, CancellationToken cancellationToken)
            => _containerGateway.SetFileSystemResourceOwnerAsync(_containerId, Path, owner, cancellationToken);

        public Task SetPermissionsAsync(string permissions, CancellationToken cancellationToken)
            => _containerGateway.SetFileSystemResourcePermissionsAsync(_containerId, Path, permissions, cancellationToken);
    }
}

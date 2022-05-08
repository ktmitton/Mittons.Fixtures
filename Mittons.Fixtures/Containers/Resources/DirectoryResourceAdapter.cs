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

        public DirectoryResourceAdapter(string containerId, string path, IContainerGateway containerGateway)
        {
            _containerId = containerId;
            Path = path;
            _containerGateway = containerGateway;
        }

        public Task CreateAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAsync(bool recursive, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<IFileResourceAdapter> GetFileAsync(string path, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<IDirectoryResourceAdapter> GetDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<IDirectoryResourceAdapter>> EnumerateDirectories(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<IFileResourceAdapter>> EnumerateFiles(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task SetPermissionsAsync(string permissions, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task SetOwnerAsync(string owner, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Core.Resources
{
    public interface IDirectoryResourceAdapter : IFileSystemResourceAdapter
    {
        Task CreateAsync(CancellationToken cancellationToken);

        Task DeleteAsync(bool recursive, CancellationToken cancellationToken);

        Task<IFileResourceAdapter> GetFileAsync(string path, CancellationToken cancellationToken);

        Task<IDirectoryResourceAdapter> GetDirectoryAsync(string path, CancellationToken cancellationToken);

        Task<IEnumerable<IDirectoryResourceAdapter>> EnumerateDirectories(CancellationToken cancellationToken);

        Task<IEnumerable<IFileResourceAdapter>> EnumerateFiles(CancellationToken cancellationToken);
    }
}

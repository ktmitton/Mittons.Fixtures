using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Core.Resources
{
    public interface IDirectoryResourceAdapter : IFileSystemResourceAdapter
    {
        Task CreateAsync(CancellationToken cancellationToken);

        Task DeleteAsync(bool recursive, CancellationToken cancellationToken);

        Task<IEnumerable<IDirectoryResourceAdapter>> EnumerateDirectoriesAsync(CancellationToken cancellationToken);

        Task<IEnumerable<IFileResourceAdapter>> EnumerateFilesAsync(CancellationToken cancellationToken);
    }
}

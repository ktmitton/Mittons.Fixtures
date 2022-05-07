using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Core.Resources
{
    public interface IDirectoryResourceAdapter : IFileSystemResourceAdapter
    {
        Task CreateAsync();

        Task DeleteAsync(bool recursive);

        Task<IFileResourceAdapter> GetFileAsync(string path);

        Task<IDirectoryResourceAdapter> GetDirectoryAsync(string path);

        Task<IEnumerable<IDirectoryResourceAdapter>> EnumerateDirectories();

        Task<IEnumerable<IDirectoryResourceAdapter>> EnumerateFiles();
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Core.Resources
{
    public interface IFileResourceAdapter : IFileSystemResourceAdapter
    {
        Task CreateAsync(CancellationToken cancellationToken);

        Task WriteAsync(string contents, CancellationToken cancellationToken);

        Task AppendAsync(string contents, CancellationToken cancellationToken);

        Task DeleteAsync(CancellationToken cancellationToken);

        Task<string> ReadAsync(CancellationToken cancellationToken);
    }
}

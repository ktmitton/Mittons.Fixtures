using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Adapters
{
    public interface IFileUriAdapter : IUriAdapter
    {
        Task WriteAsync(string contents, CancellationToken cancellationToken);

        Task SetOwnerAsync(string owner, CancellationToken cancellationToken);

        Task SetPermissionsAsync(string permissions, CancellationToken cancellationToken);

        Task DeleteAsync(CancellationToken cancellationToken);
    }
}

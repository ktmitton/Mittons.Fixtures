using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Core.Resources
{
    public interface IFileSystemResourceAdapter : IResourceAdapter
    {
        Task SetPermissionsAsync(string permissions, CancellationToken cancellationToken);

        Task SetOwnerAsync(string owner, CancellationToken cancellationToken);
    }
}

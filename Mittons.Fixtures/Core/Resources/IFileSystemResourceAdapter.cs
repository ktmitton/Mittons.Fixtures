using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Core.Resources
{
    public interface IFileSystemResourceAdapter : IResourceAdapter
    {
        string Path { get; }

        Task SetPermissionsAsync(string permissions, CancellationToken cancellationToken);

        Task SetOwnerAsync(string owner, CancellationToken cancellationToken);
    }
}

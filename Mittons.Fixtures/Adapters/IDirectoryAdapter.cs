using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Adapters
{
    public interface IDirectoryUriAdapter : IUriAdapter
    {
        Task EmptyAsync(CancellationToken cancellation);
    }
}

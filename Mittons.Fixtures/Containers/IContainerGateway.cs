using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Containers
{
    public interface IContainerGateway
    {
        Task<string> CreateContainerAsync(Dictionary<string, string> labels, CancellationToken cancellationToken);

        Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken);
    }
}

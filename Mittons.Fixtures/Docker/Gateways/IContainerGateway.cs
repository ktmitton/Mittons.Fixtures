using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Models;
using Mittons.Fixtures.Resources;

namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IContainerGateway : IServiceGateway<IDockerService>
    {
        Task<string> RunAsync(string imageName, string command, IEnumerable<Option> options, CancellationToken cancellationToken);

        Task RemoveAsync(string containerId, CancellationToken cancellationToken);

        Task AddFileAsync(string containerId, string hostFilename, string containerFilename, string owner, string permissions, CancellationToken cancellationToken);

        Task RemoveFileAsync(string containerId, string containerFilename, CancellationToken cancellationToken);

        Task EmptyDirectoryAsync(string containerId, string directory, CancellationToken cancellationToken);

        Task<IEnumerable<string>> ExecuteCommandAsync(string containerId, string command, CancellationToken cancellationToken);

        Task<HealthStatus> GetHealthStatusAsync(string containerId, CancellationToken cancellationToken);
    }
}
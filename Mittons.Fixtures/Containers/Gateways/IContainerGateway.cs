using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Gateways
{
    public interface IContainerGateway
    {
        Task<string> CreateContainerAsync(string imageName, PullOption pullOption, string network, string networkAlias, Dictionary<string, string> labels, Dictionary<string, string> environmentVariables, string hostname, string command, IHealthCheckDescription healthCheckDescription, CancellationToken cancellationToken);

        Task BuildImageAsync(string dockerfilePath, string target, bool pullDependencyImages, string imageName, string context, string arguments, CancellationToken cancellationToken);

        Task EnsureContainerIsHealthyAsync(string containerId, CancellationToken cancellationToken);

        Task<IEnumerable<IResource>> GetAvailableResourcesAsync(string containerId, CancellationToken cancellationToken);

        Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken);

        Task SetFileSystemResourceOwnerAsync(string containerId, string path, string owner, CancellationToken cancellationToken);

        Task SetFileSystemResourcePermissionsAsync(string containerId, string path, string permissions, CancellationToken cancellationToken);

        Task CreateFileAsync(string containerId, string path, CancellationToken cancellationToken);

        Task DeleteFileAsync(string containerId, string path, CancellationToken cancellationToken);

        Task AppendFileAsync(string containerId, string path, string contents, CancellationToken cancellationToken);

        Task WriteFileAsync(string containerId, string path, string contents, CancellationToken cancellationToken);

        Task<string> ReadFileAsync(string containerId, string path, CancellationToken cancellationToken);

        Task CreateDirectoryAsync(string containerId, string path, CancellationToken cancellationToken);

        Task DeleteDirectoryAsync(string containerId, string path, bool recursive, CancellationToken cancellationToken);

        Task<IEnumerable<IDirectoryResourceAdapter>> EnumerateDirectoriesAsync(string containerId, string path, CancellationToken cancellationToken);

        Task<IEnumerable<IFileResourceAdapter>> EnumerateFilesAsync(string containerId, string path, CancellationToken cancellationToken);
    }
}

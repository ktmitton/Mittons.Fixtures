using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration.Containers;

public class DockerCleanupFixture : IAsyncLifetime
{
    private readonly List<string> _containerIds;

    private readonly List<string> _networkIds;

    public DockerCleanupFixture()
    {
        _containerIds = new List<string>();
        _networkIds = new List<string>();
    }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public Task DisposeAsync()
        => Task.WhenAll(_containerIds.Select(x => RemoveContainer(x)).ToList());

    public void AddContainer(string containerId)
        => _containerIds.Add(containerId);

    public void AddNetwork(string networkId)
        => _networkIds.Add(networkId);

    private async Task RemoveContainer(string containerId)
    {
        using var process = new Process();
        process.StartInfo.FileName = "docker";
        process.StartInfo.Arguments = $"rm --force {containerId}";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;

        process.Start();
        await process.WaitForExitAsync().ConfigureAwait(false);
    }
}

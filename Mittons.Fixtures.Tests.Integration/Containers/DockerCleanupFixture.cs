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

    public async Task DisposeAsync()
    {
        await Task.WhenAll(_containerIds.Select(x => RemoveContainer(x)));
        await Task.WhenAll(_networkIds.Select(x => RemoveNetwork(x)));
    }

    public void AddContainer(string containerId)
        => _containerIds.Add(containerId);

    public void AddNetwork(string networkId)
        => _networkIds.Add(networkId);

    private async Task RemoveContainer(string containerId)
    {
        using var process = new Process();
        process.StartInfo.FileName = "docker";
        process.StartInfo.Arguments = $"rm -v --force {containerId}";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;

        process.Start();
        await process.WaitForExitAsync().ConfigureAwait(false);
    }

    private async Task RemoveNetwork(string networkId)
    {
        using var process = new Process();
        process.StartInfo.FileName = "docker";
        process.StartInfo.Arguments = $"network rm {networkId}";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;

        process.Start();
        await process.WaitForExitAsync().ConfigureAwait(false);
        // var temp = process.StandardOutput.ReadToEnd();
        // Assert.Equal(string.Empty, temp);
        // var a = 1;
    }
}

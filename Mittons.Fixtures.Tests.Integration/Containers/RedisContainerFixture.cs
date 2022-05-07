using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration.Containers;

public class RedisContainerFixture : IAsyncLifetime
{
    public string ContainerId { get; private set; } = string.Empty;

    public RedisContainerFixture()
    {
    }

    public async Task InitializeAsync()
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = $"run -d redis:alpine";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            await process.WaitForExitAsync().ConfigureAwait(false);

            ContainerId = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false) ?? string.Empty;
        }
    }

    public async Task DisposeAsync()
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = $"rm -v --force {ContainerId}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
    }
}

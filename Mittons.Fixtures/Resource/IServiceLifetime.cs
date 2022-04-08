using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures
{
    public interface IServiceLifetime
    {
        Task<string> StartAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task<HealthStatus> GetHealthStatusAsync();
    }

    public enum HealthStatus
    {
        Unknown,
        Running,
        Healthy,
        Unhealthy
    }
}
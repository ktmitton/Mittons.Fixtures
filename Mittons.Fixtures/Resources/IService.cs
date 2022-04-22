using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Resources
{
    public interface IServiceGateway<TService> where TService : IService
    {
        Task<IEnumerable<IServiceAccessPoint>> GetServiceAccessPointsAsync(TService service, CancellationToken cancellationToken);
    }

    public interface IService : IAsyncLifetime
    {
        IEnumerable<IServiceAccessPoint> ServiceAccessPoints { get; }
    }

    public interface IDockerService : IService
    {
        string Id { get; }
    }

    public interface IServiceAccessPoint
    {
        Uri LocalUri { get; }

        Uri PublicUri { get; }
    }
}
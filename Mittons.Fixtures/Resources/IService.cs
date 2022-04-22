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

    /// <summary>
    /// Represents details needed for the Host environment to communicate with the Guest <see cref="Mittons.Fixtures.Resources.IService"/>.
    /// </summary>
    /// <remarks>
    /// These communications details can be a variety of mechanisms, such as file changes or network messaging.
    /// </remarks>
    public interface IServiceAccessPoint
    {
        /// <summary>
        /// Gets the details for how the <see cref="Mittons.Fixtures.Resources.IService"/> monitors a resource to trigger actions.
        /// </summary>
        /// <remarks>
        /// This can represent asynchronous communication mechanisms such as monitoring a file for changes or more synchronous request-respones channels such as http connections.
        /// </remarks>
        /// <returns>
        /// Details describing the resource being monitored by the <see cref="Mittons.Fixtures.Resources.IServiceAccessPoint"/>.
        /// </returns>
        /// <value>
        /// A <see href="https://en.wikipedia.org/wiki/Uniform_Resource_Identifier">Uniform Resource Identifier</see> with all known details on how the <see cref="Mittons.Fixtures.Resources.IServiceAccessPoint"/> listens for action triggers.
        /// </value>
        Uri GuestUri { get; }

        /// <summary>
        /// Gets the details for how the Host can communicate with an <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </summary>
        /// <remarks>
        /// This can represent asynchronous communication mechanisms such as monitoring a file for changes or more synchronous request-respones channels such as http connections.
        /// </remarks>
        /// <returns>
        /// Details describing the resource that can be accessed by the Host to communicate with the <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </returns>
        /// <value>
        /// A <see href="https://en.wikipedia.org/wiki/Uniform_Resource_Identifier">Uniform Resource Identifier</see> with all known details on how the Host communicates with the <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </value>
        Uri HostUri { get; }
    }
}
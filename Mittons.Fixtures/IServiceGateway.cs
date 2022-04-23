using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures
{
    /// <summary>
    /// A gateway for managing <see cref="Mittons.Fixtures.Resources.IService">IServices</see>.
    /// </summary>
    /// <remarks>
    /// Handles creating and disposing of <see cref="Mittons.Fixtures.Resources.IService">Guest services</see>, as well as providing details for <see cref="Mittons.Fixtures.Resources.IServiceResource">resources</see> being monitored through which the Host can communicate with the Guest.
    /// </remarks>
    public interface IServiceGateway<TService> where TService : IService
    {
        /// <summary>
        /// Creates an instance of an <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </summary>
        /// <param name="attributes">
        /// The <see cref="System.Attribute">Attributes</see> defining the parameters of the <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token to cancel the operation.
        /// </param>
        /// <exception cref="System.OperationCanceledException">If the <see cref="Mittons.Fixtures.Resources.IServiceGateway"/> supports it, this exception may be thrown if the <paramref name="cancellationToken"/> is cancelled before the operation can complete.</exception>
        /// <remarks>
        /// The provided <see cref="Mittons.Fixtures.Resources.IService"/> should not be intialized yet, and should contain all details needed by the <see cref="Mittons.Fixtures.Resources.IServiceGateway"/> to create a new instance.
        /// </remarks>
        Task<TService> CreateServiceAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken);

        /// <summary>
        /// Stops and removes an <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </summary>
        /// <param name="service">
        /// The <see cref="Mittons.Fixtures.Resources.IService"/> that should be stopped and removed from the Host system.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token to cancel the operation.
        /// </param>
        /// <exception cref="System.OperationCanceledException">If the <see cref="Mittons.Fixtures.Resources.IServiceGateway"/> supports it, this exception may be thrown if the <paramref name="cancellationToken"/> is cancelled before the operation can complete.</exception>
        /// <remarks>
        /// This operation will remove the <see cref="Mittons.Fixtures.Resources.IService"/> and release all resources it held on the Host system.
        /// </remarks>
        Task RemoveServiceAsync(TService service, CancellationToken cancellationToken);
    }
}

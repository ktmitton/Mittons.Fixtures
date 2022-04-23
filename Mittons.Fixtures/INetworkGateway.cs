using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Attributes;

namespace Mittons.Fixtures
{
    /// <summary>
    /// A gateway for managing <see cref="Mittons.Fixtures.INetwork">INetworks</see>.
    /// </summary>
    /// <remarks>
    /// Handles creating and disposing of <see cref="Mittons.Fixtures.INetwork">Guest networks</see>, as well connecting instances of <see cref="Mittons.Fixtures.IService"/> to the Guest networks.
    /// </remarks>
    public interface INetworkGateway<TNetwork> where TNetwork : INetwork
    {
        /// <summary>
        /// Creates an instance of an <see cref="Mittons.Fixtures.INetwork"/>.
        /// </summary>
        /// <param name="attributes">
        /// The <see cref="System.Attribute">Attributes</see> defining the parameters of the <see cref="Mittons.Fixtures.INetwork"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token to cancel the operation.
        /// </param>
        /// <exception cref="System.OperationCanceledException">If the <see cref="Mittons.Fixtures.INetworkGateway"/> supports it, this exception may be thrown if the <paramref name="cancellationToken"/> is cancelled before the operation can complete.</exception>
        /// <remarks>
        /// The provided <see cref="Mittons.Fixtures.INetwork"/> should not be intialized yet, and should contain all details needed by the <see cref="Mittons.Fixtures.INetworkGateway"/> to create a new instance.
        /// </remarks>
        Task<TNetwork> CreateNetworkAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken);

        /// <summary>
        /// Stops and removes an <see cref="Mittons.Fixtures.INetwork"/>.
        /// </summary>
        /// <param name="network">
        /// The <see cref="Mittons.Fixtures.INetwork"/> that should be stopped and removed from the Host system.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token to cancel the operation.
        /// </param>
        /// <exception cref="System.OperationCanceledException">If the <see cref="Mittons.Fixtures.INetworkGateway"/> supports it, this exception may be thrown if the <paramref name="cancellationToken"/> is cancelled before the operation can complete.</exception>
        /// <remarks>
        /// This operation will remove the <see cref="Mittons.Fixtures.INetwork"/> and release all resources it held on the Host system.
        /// </remarks>
        Task RemoveNetworkAsync(TNetwork network, CancellationToken cancellationToken);

        /// <summary>
        /// Connects an <see cref="Mittons.Fixtures.IService"/> to an <see cref="Mittons.Fixtures.INetwork"/>.
        /// </summary>
        /// <param name="network">
        /// The <see cref="Mittons.Fixtures.INetwork"/> to which the <see cref="Mittons.Fixtures.IService"/> should be connected.
        /// </param>
        /// <param name="service">
        /// The <see cref="Mittons.Fixtures.IService"/> to connect.
        /// </param>
        /// <param name="alias">
        /// The details about how the <see cref="Mittons.Fixtures.IService"/> should be identified within the <see cref="Mittons.Fixtures.INetwork"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token to cancel the operation.
        /// </param>
        /// <exception cref="System.OperationCanceledException">If the <see cref="Mittons.Fixtures.INetworkGateway"/> supports it, this exception may be thrown if the <paramref name="cancellationToken"/> is cancelled before the operation can complete.</exception>
        /// <exception cref="System.NotSupportedException">Thrown if the <see cref="Mittons.Fixtures.INetworkGateway"/> does not support the provided <see cref="Mittons.Fixtures.IService"/>.</exception>
        Task ConnectServiceAsync(TNetwork network, IService service, NetworkAliasAttribute alias, CancellationToken cancellationToken);
    }
}

using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Core.Attributes;

namespace Mittons.Fixtures.Core.Services
{
    /// <summary>
    /// Represents details describing the capabilities of an <see cref="Mittons.Fixtures.INetworkService"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Mittons.Fixtures.INetworkService">INetworks</see> are used to facilitate communication between instances of <see cref="Mittons.Fixtures.IService">IServices</see>.
    /// </remarks>
    public interface INetworkService : IService
    {
        string Name { get; }

        Task ConnectAsync(NetworkAliasAttribute networkAlias, CancellationToken cancellationToken);
    }
}

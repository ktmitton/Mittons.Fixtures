using System;

namespace Mittons.Fixtures
{
    public interface INetworkGatewayFactory
    {
        INetworkGateway<INetwork> GetNetworkGateway(Type networkType);
    }
}

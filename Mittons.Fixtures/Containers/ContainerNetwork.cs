namespace Mittons.Fixtures.Containers
{
    internal class ContainerNetwork : IContainerNetwork
    {
        public string NetworkId { get; }

        public ContainerNetwork(string networkId)
        {
            NetworkId = networkId;
        }
    }
}

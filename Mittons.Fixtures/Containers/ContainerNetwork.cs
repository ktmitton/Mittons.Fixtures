namespace Mittons.Fixtures.Containers
{
    internal class ContainerNetwork : IContainerNetwork
    {
        public string NetworkId { get; }

        public string Name { get; }

        public ContainerNetwork(string networkId, string name)
        {
            NetworkId = networkId;
            Name = name;
        }
    }
}

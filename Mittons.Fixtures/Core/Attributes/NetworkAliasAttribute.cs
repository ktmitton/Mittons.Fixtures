using System;
using Mittons.Fixtures.Core.Services;

namespace Mittons.Fixtures.Core.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class NetworkAliasAttribute : Attribute
    {
        public string NetworkName { get; }

        public string Alias { get; }

        public bool IsExternalNetwork { get; }

        public INetworkService NetworkService { get; set; }

        public IService ConnectedService { get; set; }

        public NetworkAliasAttribute(string networkName, string alias)
            : this(networkName, alias, false)
        {
        }

        public NetworkAliasAttribute(string networkName, string alias, bool isExternalNetwork)
        {
            NetworkName = networkName;
            Alias = alias;
            IsExternalNetwork = isExternalNetwork;
        }
    }
}

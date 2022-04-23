using System;

namespace Mittons.Fixtures.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class NetworkAliasAttribute : Attribute
    {
        public string NetworkName { get; }

        public string Alias { get; }

        public bool IsExternalNetwork { get; }

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

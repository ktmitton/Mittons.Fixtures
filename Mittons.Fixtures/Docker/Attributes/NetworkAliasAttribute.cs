using System;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class NetworkAliasAttribute : Attribute
    {
        public string NetworkName { get; }

        public string Alias { get; }

        public NetworkAliasAttribute(string networkName, string alias)
        {
            NetworkName = networkName;
            Alias = alias;
        }
    }
}
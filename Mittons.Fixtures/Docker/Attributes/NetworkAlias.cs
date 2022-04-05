using System;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class NetworkAlias : Attribute
    {
        public string NetworkName { get; }

        public string Alias { get; }

        public NetworkAlias(string networkName, string alias)
        {
            NetworkName = networkName;
            Alias = alias;
        }
    }
}
using System;

namespace Mittons.Fixtures.Containers.Attributes
{
    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class NetworkAttribute : Attribute
    {
        public string Name { get; }

        public NetworkAttribute(string name)
        {
            Name = name;
        }
    }
}
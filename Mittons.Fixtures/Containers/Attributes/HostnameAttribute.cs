using System;

namespace Mittons.Fixtures.Containers.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class HostnameAttribute : Attribute
    {
        public string Name { get; set; }

        public HostnameAttribute(string name)
        {
            Name = name;
        }
    }
}

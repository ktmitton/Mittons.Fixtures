using System;

namespace Mittons.Fixtures.Core.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class ServiceDependencyAttribute : Attribute
    {
        public string Name { get; }

        public ServiceDependencyAttribute(string name)
        {
            Name = name;
        }
    }
}

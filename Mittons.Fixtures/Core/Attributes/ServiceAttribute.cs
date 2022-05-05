using System;

namespace Mittons.Fixtures.Core.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class ServiceAttribute : Attribute
    {
        public string Name { get; }

        public ServiceAttribute(string name)
        {
            Name = name;
        }
    }
}

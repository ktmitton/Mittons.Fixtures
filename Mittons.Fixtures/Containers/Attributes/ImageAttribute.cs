using System;

namespace Mittons.Fixtures.Containers.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class ImageAttribute : Attribute
    {
        public string Name { get; }

        public ImageAttribute(string name)
        {
            Name = name;
        }
    }
}
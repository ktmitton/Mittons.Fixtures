using System;

namespace Mittons.Fixtures.Docker.Containers
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class Image : Attribute
    {
        public string Name { get; }

        public Image(string name)
        {
            Name = name;
        }
    }
}
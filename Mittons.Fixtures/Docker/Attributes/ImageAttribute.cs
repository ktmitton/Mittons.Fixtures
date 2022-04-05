using System;

namespace Mittons.Fixtures.Docker.Containers
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class Image : Attribute
    {
        public string ImageName { get; }

        public Image(string imageName)
        {
            ImageName = imageName;
        }
    }
}
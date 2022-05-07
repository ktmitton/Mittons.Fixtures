using System;

namespace Mittons.Fixtures.Containers.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class ImageAttribute : Attribute
    {
        public string Name { get; set; }

        public PullOption PullOption { get; set; }

        public ImageAttribute(string name) : this(name, PullOption.Missing)
        {
        }

        public ImageAttribute(string name, PullOption pullOption)
        {
            Name = name;
            PullOption = pullOption;
        }
    }

    public enum PullOption
    {
        Always,
        Missing,
        Never
    }
}
using System;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class Scale : Attribute
    {
        public int Count { get; }

        public Scale(int count)
        {
            Count = count;
        }
    }
}
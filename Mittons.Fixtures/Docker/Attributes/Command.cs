using System;

namespace Mittons.Fixtures.Docker.Containers
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class Command : Attribute
    {
        public string Value { get; }

        public Command(string value)
        {
            Value = value;
        }
    }
}
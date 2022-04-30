using System;

namespace Mittons.Fixtures.Core.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public string Value { get; }

        public CommandAttribute(string value)
        {
            Value = value;
        }
    }
}
using System;

namespace Mittons.Fixtures.Core.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class EnvironmentVariableAttribute : Attribute
    {
        public string Key { get; }

        public string Value { get; }

        public EnvironmentVariableAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}

using System;

namespace Mittons.Fixtures.Core.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
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

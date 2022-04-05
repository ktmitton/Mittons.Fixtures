using System;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class Network : Attribute
    {
        public string Name { get; }

        public Network(string name)
        {
            Name = name;
        }
    }
}
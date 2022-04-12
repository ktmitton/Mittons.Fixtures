using System;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class Build : Attribute
    {
        public string Dockerfile { get; }

        public string Context { get; }

        public Build(string dockerfile, string context)
        {
            Dockerfile = dockerfile;
            Context = context;
        }
    }
}
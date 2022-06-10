using System;

namespace Mittons.Fixtures.Containers.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class BuildAttribute : Attribute
    {
        public string DockerfilePath { get; set; }

        public string Target { get; set; }

        public bool PullDependencyImages { get; set; }

        public string Context { get; set; }

        public string Arguments { get; set; }

        public BuildAttribute(string dockerfilePath, string target, bool pullDependencyImages, string context, string arguments)
        {
            DockerfilePath = dockerfilePath;
            Target = target;
            PullDependencyImages = pullDependencyImages;
            Context = context;
            Arguments = arguments;
        }
    }
}
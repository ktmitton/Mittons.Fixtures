using System;
using System.Collections.Generic;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class Run : Attribute
    {
        public static string DefaultId = Guid.NewGuid().ToString();

        public string Id { get; }

        public Dictionary<string, string> Labels => new Dictionary<string, string>
        {
            { "mittons.fixtures.run.id", Id }
        };

        public Run()
        {
            Id = DefaultId;
        }

        public Run(string idEnvironmentVariableName)
        {
            var id = Environment.GetEnvironmentVariable(idEnvironmentVariableName);

            Id = string.IsNullOrWhiteSpace(id) ? DefaultId : id;
        }
    }
}
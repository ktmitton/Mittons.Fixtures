using System;
using System.Collections.Generic;
using Mittons.Fixtures.Extensions;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class RunAttribute : Attribute, IOptionAttribute
    {
        public static string DefaultId = Guid.NewGuid().ToString();

        public string Id { get; }

        public bool TeardownOnComplete { get; }

        public IEnumerable<KeyValuePair<string, string>> Options => new[]
        {
            new KeyValuePair<string, string>("--label", $"mittons.fixtures.run.id={Id}")
        };

        public RunAttribute()
            : this(DefaultId)
        {
        }

        public RunAttribute(bool teardownOnComplete)
            : this(DefaultId, teardownOnComplete)
        {
        }

        public RunAttribute(string id)
            : this(id, true)
        {
        }

        public RunAttribute(string id, bool teardownOnComplete)
        {
            var replacedId = id.ReplaceEnvironmentVariables();

            Id = string.IsNullOrWhiteSpace(replacedId) ? DefaultId : replacedId;
            TeardownOnComplete = teardownOnComplete;
        }
    }
}
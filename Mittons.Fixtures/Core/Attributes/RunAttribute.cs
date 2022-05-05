using System;
using Mittons.Fixtures.Core.Extensions;

namespace Mittons.Fixtures.Core.Attributes
{
    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class RunAttribute : Attribute
    {
        public static readonly string DefaultId = Guid.NewGuid().ToString();

        public string Id { get; }

        public bool TeardownOnComplete { get; }

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

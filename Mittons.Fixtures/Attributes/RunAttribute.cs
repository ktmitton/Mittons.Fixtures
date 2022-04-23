using System;
using System.Collections.Generic;
using Mittons.Fixtures.Extensions;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Attributes
{
    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class RunAttribute : Attribute, IOptionAttribute
    {
        public static string DefaultId = Guid.NewGuid().ToString();

        public string Id { get; }

        public bool TeardownOnComplete { get; }

        public IEnumerable<Option> Options => new List<Option>
        {
            new Option
            {
                Name = "--label",
                Value = $"mittons.fixtures.run.id={Id}"
            }
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
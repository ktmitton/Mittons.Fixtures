using System;
using System.Collections.Generic;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class HealthCheckAttribute : Attribute, IOptionAttribute
    {
        public bool Disabled { get; set; }

        public string Command { get; set; }

        public TimeSpan? Interval { get; set; }

        public TimeSpan? Timeout { get; set; }

        public TimeSpan? StartPeriod { get; set; }

        public int? Retries { get; set; }

        public IEnumerable<Option> Options
        {
            get
            {
                var options = new List<Option>();

                if (Disabled)
                {
                    options.Add(new Option { Name = "--no-healthcheck", Value = string.Empty });
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(Command))
                    {
                        options.Add(new Option { Name = "--health-cmd", Value = Command });
                    }

                    if (Interval.HasValue)
                    {
                        options.Add(new Option { Name = "--health-interval", Value = $"{Math.Ceiling(Interval.Value.TotalSeconds)}s" });
                    }

                    if (Timeout.HasValue)
                    {
                        options.Add(new Option { Name = "--health-timeout", Value = $"{Math.Ceiling(Timeout.Value.TotalSeconds)}s" });
                    }

                    if (StartPeriod.HasValue)
                    {
                        options.Add(new Option { Name = "--health-start-period", Value = $"{Math.Ceiling(StartPeriod.Value.TotalSeconds)}s" });
                    }

                    if (Retries.HasValue)
                    {
                        options.Add(new Option { Name = "--health-retries", Value = Retries.Value.ToString() });
                    }
                }

                return options;
            }
        }

        public HealthCheckAttribute()
        {
        }
    }
}
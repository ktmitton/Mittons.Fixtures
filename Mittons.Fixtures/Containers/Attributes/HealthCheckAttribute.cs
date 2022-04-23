using System;
using System.Collections.Generic;
using Mittons.Fixtures.Attributes;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Containers.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class HealthCheckAttribute : Attribute, IOptionAttribute
    {
        public bool Disabled { get; set; }

        public string Command { get; set; }

        public byte Interval { get; set; }

        public byte Timeout { get; set; }

        public byte StartPeriod { get; set; }

        public byte Retries { get; set; }

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

                    if (Interval > 0)
                    {
                        options.Add(new Option { Name = "--health-interval", Value = $"{Interval}s" });
                    }

                    if (Timeout > 0)
                    {
                        options.Add(new Option { Name = "--health-timeout", Value = $"{Timeout}s" });
                    }

                    if (StartPeriod > 0)
                    {
                        options.Add(new Option { Name = "--health-start-period", Value = $"{StartPeriod}s" });
                    }

                    if (Retries > 0)
                    {
                        options.Add(new Option { Name = "--health-retries", Value = Retries.ToString() });
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
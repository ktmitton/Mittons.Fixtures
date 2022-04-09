using System;
using System.Collections.Generic;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class HealthCheck : Attribute, IOptionAttribute
    {
        public bool Disabled { get; set; }

        public string Command { get; set; }

        public TimeSpan? Interval { get; set; }

        public TimeSpan? Timeout { get; set; }

        public TimeSpan? StartPeriod { get; set; }

        public int? Retries { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Options
        {
            get
            {
                var options = new List<KeyValuePair<string, string>>();

                if (Disabled)
                {
                    options.Add(new KeyValuePair<string, string>("--no-healthcheck", string.Empty));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(Command))
                    {
                        options.Add(new KeyValuePair<string, string>("--health-cmd", Command));
                    }

                    if (Interval.HasValue)
                    {
                        options.Add(new KeyValuePair<string, string>("--health-interval", $"{Math.Ceiling(Interval.Value.TotalSeconds)}s"));
                    }

                    if (Timeout.HasValue)
                    {
                        options.Add(new KeyValuePair<string, string>("--health-timeout", $"{Math.Ceiling(Timeout.Value.TotalSeconds)}s"));
                    }

                    if (StartPeriod.HasValue)
                    {
                        options.Add(new KeyValuePair<string, string>("--health-start-period", $"{Math.Ceiling(StartPeriod.Value.TotalSeconds)}s"));
                    }

                    if (Retries.HasValue)
                    {
                        options.Add(new KeyValuePair<string, string>("--health-retries", Retries.Value.ToString()));
                    }
                }

                return options;
            }
        }

        public HealthCheck()
        {
        }
    }
}
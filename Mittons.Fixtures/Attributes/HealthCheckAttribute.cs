using System;

namespace Mittons.Fixtures.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class HealthCheckAttribute : Attribute
    {
        public bool Disabled { get; set; }

        public string Command { get; set; }

        public byte Interval { get; set; }

        public byte Timeout { get; set; }

        public byte StartPeriod { get; set; }

        public byte Retries { get; set; }
    }
}
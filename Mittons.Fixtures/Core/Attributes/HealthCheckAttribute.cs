using System;

namespace Mittons.Fixtures.Core.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class HealthCheckAttribute : Attribute, IHealthCheckDescription
    {
        public bool Disabled { get; set; }

        public string Command { get; set; }

        public byte Interval { get; set; }

        public byte Timeout { get; set; }

        public byte StartPeriod { get; set; }

        public byte Retries { get; set; }
    }

    public interface IHealthCheckDescription
    {
        bool Disabled { get; }

        string Command { get; }

        byte Interval { get; }

        byte Timeout { get; }

        byte StartPeriod { get; }

        byte Retries { get; }
    }
}
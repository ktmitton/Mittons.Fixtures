using System;
using System.Collections.Generic;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class PortBinding : Attribute, IOptionAttribute
    {
        public Protocol Protocol { get; set; } = Protocol.Tcp;

        public string Scheme { get; set; }

        public int Port { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Options
        {
            get
            {
                var protocol = Protocol == Protocol.Tcp ? "tcp" : "udp";

                return new[] { new KeyValuePair<string, string>("-p", $"{Port}/{protocol}") };
            }
        }
    }

    public enum Protocol
    {
        Tcp,
        Udp
    }
}
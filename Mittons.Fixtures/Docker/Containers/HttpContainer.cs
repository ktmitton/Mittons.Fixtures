using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Containers
{
    public class HttpContainer : Container
    {
        private static HttpMessageHandler HttpMessageHandler = new HttpClientHandler()
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (a, b, c, d) => true
        };

        public HttpClient UnsecureHttpClient { get; }

        public HttpClient SecureHttpClient { get; }

        private readonly int unsecurePort;

        private readonly int securePort;

        public HttpContainer(IDockerGateway dockerGateway, Guid instanceId, IEnumerable<Attribute> attributes)
            : base(dockerGateway, instanceId, GetAttributesWithDefaults(attributes))
        {
            UnsecureHttpClient = new HttpClient(HttpMessageHandler, false);
            SecureHttpClient = new HttpClient(HttpMessageHandler, false);

            var fullAttributes = GetAttributesWithDefaults(attributes);

            unsecurePort = fullAttributes.OfType<PortBinding>().Single(x => x.Protocol == Protocol.Tcp && x.Scheme == "http").Port;
            securePort = fullAttributes.OfType<PortBinding>().Single(x => x.Protocol == Protocol.Tcp && x.Scheme == "https").Port;
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            UnsecureHttpClient.BaseAddress = await GetBaseAddress("http", unsecurePort);
            SecureHttpClient.BaseAddress = await GetBaseAddress("https", securePort);
        }

        private async Task<Uri> GetBaseAddress(string scheme, int port)
        {
            var host = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : IpAddress.ToString();
            var boundPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? await _dockerGateway.ContainerGetHostPortMappingAsync(Id, "tcp", port, CancellationToken.None) : port;

            return new Uri($"{scheme}://{host}:{boundPort}");
        }
        private static IEnumerable<Attribute> GetAttributesWithDefaults(IEnumerable<Attribute> attributes)
        {
            var fullAttributes = attributes;

            if (!fullAttributes.OfType<PortBinding>().Any(x => x.Protocol == Protocol.Tcp && x.Scheme == "http"))
            {
                fullAttributes = fullAttributes.Concat(new[] { new PortBinding { Protocol = Protocol.Tcp, Scheme = "http", Port = 80 } });
            }

            if (!fullAttributes.OfType<PortBinding>().Any(x => x.Protocol == Protocol.Tcp && x.Scheme == "https"))
            {
                fullAttributes = fullAttributes.Concat(new[] { new PortBinding { Protocol = Protocol.Tcp, Scheme = "https", Port = 443 } });
            }

            return fullAttributes;
        }
    }
}
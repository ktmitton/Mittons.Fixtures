using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Docker.Gateways
{
    internal class DockerContainerGateway : IContainerGateway
    {
        public async Task<string> CreateContainerAsync(string imageName, Dictionary<string, string> labels, CancellationToken cancellationToken)
        {
            var labelOptions = string.Join(" ", labels.Select(x => $"--label \"{x.Key}={x.Value}\""));

            using (var process = new DockerProcess($"run -d -P {labelOptions} {imageName}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var containerId = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

                return containerId;
            }
        }

        public async Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"rm --force {containerId}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<string> GetServiceIpAddress(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{.NetworkSettings.IPAddress}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                return await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<IResource>> GetAvailableResourcesAsync(string containerId, CancellationToken cancellationToken)
        {
            var ipAddress = await GetServiceIpAddress(containerId, cancellationToken).ConfigureAwait(false);

            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{json .NetworkSettings.Ports}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                var ports = JsonSerializer.Deserialize<Dictionary<string, Port[]>>(output) ?? new Dictionary<string, Port[]>();
                var publicHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : ipAddress;

                return ports.Select(x =>
                {
                    var publicPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? x.Value.First().HostPort : x.Key.Split('/').First();

                    return new Resource(
                        new Uri($"{x.Key.Split('/').Last()}://localhost:{x.Key.Split('/').First()}"),
                        new Uri($"{x.Key.Split('/').Last()}://{publicHost}:{publicPort}")
                    );
                }).ToArray();
            }
        }

        private class Port
        {
            public string HostIp { get; set; }

            public string HostPort { get; set; }
        }

        private class Resource : IResource
        {
            public Uri GuestUri { get; }

            public Uri HostUri { get; }

            public Resource(Uri guestUri, Uri hostUri)
            {
                GuestUri = guestUri;
                HostUri = hostUri;
            }
        }
    }
}

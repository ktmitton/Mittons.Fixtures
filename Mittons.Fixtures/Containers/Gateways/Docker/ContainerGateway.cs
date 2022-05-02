using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Gateways.Docker
{
    internal class ContainerGateway : IContainerGateway
    {
        public async Task<string> CreateContainerAsync(string imageName, Dictionary<string, string> labels, string command, IHealthCheckDescription healthCheckDescription, CancellationToken cancellationToken)
        {
            var labelOptions = string.Join(" ", labels.Select(x => $"--label \"{x.Key}={x.Value}\""));

            var healthCheck = string.Empty;

            if (healthCheckDescription?.Disabled ?? false)
            {
                healthCheck = "--no-healthcheck";
            }
            else if (!(healthCheckDescription is null))
            {
                healthCheck = $"--health-cmd \"{healthCheckDescription.Command}\" --health-interval \"{healthCheckDescription.Interval}s\" --health-timeout \"{healthCheckDescription.Timeout}s\" --health-start-period \"{healthCheckDescription.StartPeriod}s\" --health-retries \"{healthCheckDescription.Retries}\"";
            }

            using (var process = new DockerProcess($"run -d -P {labelOptions} {healthCheck} {imageName} {command}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var containerId = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

                return containerId;
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
                var hostHostname = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : ipAddress;

                return ports.Select(x =>
                {
                    var guestPortDetails = x.Key.Split('/');
                    var guestPort = guestPortDetails.First();
                    var guestScheme = guestPortDetails.Last();
                    var guestHostname = "localhost";

                    var hostPortDetails = x.Value.First();
                    var hostPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? hostPortDetails.HostPort : guestPort;

                    return new Resource(
                        new Uri($"{guestScheme}://{guestHostname}:{guestPort}"),
                        new Uri($"{guestScheme}://{hostHostname}:{hostPort}")
                    );
                }).ToArray();
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Extensions;
using Mittons.Fixtures.Models;
using Mittons.Fixtures.Resources;

namespace Mittons.Fixtures.Docker.Gateways
{
    public class ContainerGateway : IContainerGateway, IServiceGateway<IDockerService>
    {
        public async Task<string> RunAsync(string imageName, string command, IEnumerable<Option> options, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"run -P -d {options.ToExecutionParametersFormattedString()} {imageName} {command}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var containerId = default(string);

                while (!process.StandardOutput.EndOfStream)
                {
                    containerId = process.StandardOutput.ReadLine();
                }

                return containerId ?? string.Empty;
            }
        }

        public async Task RemoveAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"rm --force {containerId}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IPAddress> GetDefaultNetworkIpAddressAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{range .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" .IPAddress}}}}{{{{end}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                IPAddress.TryParse(process.StandardOutput.ReadLine(), out var expectedIpAddress);

                return expectedIpAddress;
            }
        }

        public async Task AddFileAsync(string containerId, string hostFilename, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
        {
            var directory = System.IO.Path.GetDirectoryName(containerFilename).Replace("\\", "/");

            using (var process = new DockerProcess($"exec {containerId} mkdir -p \"{directory}\" >/dev/null 2>&1"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }

            using (var process = new DockerProcess($"cp \"{hostFilename}\" \"{containerId}:{containerFilename}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                using (var process = new DockerProcess($"exec {containerId} chown {owner} \"{containerFilename}\""))
                {
                    await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            if (!string.IsNullOrWhiteSpace(permissions))
            {
                using (var process = new DockerProcess($"exec {containerId} chmod {permissions} \"{containerFilename}\""))
                {
                    await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task RemoveFileAsync(string containerId, string containerFilename, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"exec {containerId} rm \"{containerFilename}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task EmptyDirectoryAsync(string containerId, string directory, CancellationToken cancellationToken)
        {
            var files = await ExecuteCommandAsync(containerId, $"ls {directory}", cancellationToken).ConfigureAwait(false);

            foreach (var file in files)
            {
                await ExecuteCommandAsync(containerId, $"rm -rf {directory}/{file}", cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<string>> ExecuteCommandAsync(string containerId, string command, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"exec {containerId} {command}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var lines = new List<string>();

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                    lines.Add(line);
                }

                return lines;
            }
        }

        public async Task<int> GetHostPortMappingAsync(string containerId, string protocol, int containerPort, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"port {containerId} {containerPort}/{protocol}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                int.TryParse((await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)).Split(':').Last(), out var port);

                return port;
            }
        }

        public async Task<HealthStatus> GetHealthStatusAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {containerId} -f \"{{{{if .State.Health}}}}{{{{.State.Health.Status}}}}{{{{else}}}}{{{{.State.Status}}}}{{{{end}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                switch (await process.StandardOutput.ReadLineAsync().ConfigureAwait(false))
                {
                    case "running":
                        return HealthStatus.Running;
                    case "healthy":
                        return HealthStatus.Healthy;
                    case "unhealthy":
                        return HealthStatus.Unhealthy;
                    default:
                        return HealthStatus.Unknown;
                }
            }
        }

        private async Task<string> GetServiceIpAddress(IDockerService service, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {service.Id} --format \"{{{{.NetworkSettings.IPAddress}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                return await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<IServiceResource>> GetServiceResources(IDockerService service, CancellationToken cancellationToken)
        {
            var ipAddress = await GetServiceIpAddress(service, cancellationToken).ConfigureAwait(false);

            using (var process = new DockerProcess($"inspect {service.Id} --format \"{{{{json .NetworkSettings.Ports}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                var ports = JsonSerializer.Deserialize<Dictionary<string, Port[]>>(output) ?? new Dictionary<string, Port[]>();
                var publicHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : ipAddress;

                return ports.Select(x =>
                {
                    var publicPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? x.Value.First().HostPort : x.Key.Split('/').First();

                    return new ServiceResource(
                        new Uri($"{x.Key.Split('/').Last()}://127.0.0.1:{x.Key.Split('/').First()}"),
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

        private class ServiceResource : IServiceResource
        {
            public Uri GuestUri { get; }

            public Uri HostUri { get; }

            public ServiceResource(Uri guestUri, Uri hostUri)
            {
                GuestUri = guestUri;
                HostUri = hostUri;
            }
        }
    }
}
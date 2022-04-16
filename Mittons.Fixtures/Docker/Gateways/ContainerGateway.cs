using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Extensions;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Gateways
{
    public class ContainerGateway : IContainerGateway
    {
        private static TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

        public async Task<string> RunAsync(string imageName, string command, IEnumerable<Option> options, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"run -P -d {options.ToExecutionParametersFormattedString()} {imageName} {command}"))
            {
                await process.RunProcessAsync(cancellationToken);

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
                await process.RunProcessAsync(cancellationToken);
            }
        }

        public async Task<IPAddress> GetDefaultNetworkIpAddressAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{range .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" .IPAddress}}}}{{{{end}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));

                IPAddress.TryParse(process.StandardOutput.ReadLine(), out var expectedIpAddress);

                return expectedIpAddress;
            }
        }

        public async Task AddFileAsync(string containerId, string hostFilename, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
        {
            var directory = System.IO.Path.GetDirectoryName(containerFilename).Replace("\\", "/");

            using (var process = new DockerProcess($"exec {containerId} mkdir -p \"{directory}\" >/dev/null 2>&1"))
            {
                await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }

            using (var process = new DockerProcess($"cp \"{hostFilename}\" \"{containerId}:{containerFilename}\""))
            {
                await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                using (var process = new DockerProcess($"exec {containerId} chown {owner} \"{containerFilename}\""))
                {
                    await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
                }
            }

            if (!string.IsNullOrWhiteSpace(permissions))
            {
                using (var process = new DockerProcess($"exec {containerId} chmod {permissions} \"{containerFilename}\""))
                {
                    await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
                }
            }
        }

        public async Task RemoveFileAsync(string containerId, string containerFilename, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"exec {containerId} rm \"{containerFilename}\""))
            {
                await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }
        }

        public async Task<IEnumerable<string>> ExecuteCommandAsync(string containerId, string command, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"exec {containerId} {command}"))
            {
                await process.RunProcessAsync(cancellationToken);

                var lines = new List<string>();

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    lines.Add(line);
                }

                return lines;
            }
        }

        public async Task<int> GetHostPortMappingAsync(string containerId, string protocol, int containerPort, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"port {containerId} {containerPort}/{protocol}"))
            {
                await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));

                int.TryParse((await process.StandardOutput.ReadLineAsync()).Split(':').Last(), out var port);

                return port;
            }
        }

        public async Task<HealthStatus> GetHealthStatusAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {containerId} -f \"{{{{if .State.Health}}}}{{{{.State.Health.Status}}}}{{{{else}}}}{{{{.State.Status}}}}{{{{end}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));

                switch (await process.StandardOutput.ReadLineAsync())
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
    }
}
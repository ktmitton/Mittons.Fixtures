using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Extensions;

namespace Mittons.Fixtures.Docker.Gateways
{
    public class DefaultDockerGateway : IDockerGateway
    {
        private static TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

        public async Task<string> ContainerRunAsync(string imageName, string command, IEnumerable<KeyValuePair<string, string>> options, CancellationToken cancellationToken)
        {
            var formattedOptions = options.Select(x => $"{x.Key} \"{x.Value}\"");

            using (var process = CreateDockerProcess($"run -P -d {string.Join(" ", formattedOptions)} {imageName} {command}"))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));

                var containerId = default(string);

                while (!process.StandardOutput.EndOfStream)
                {
                    containerId = process.StandardOutput.ReadLine();
                }

                return containerId ?? string.Empty;
            }
        }

        public async Task ContainerRemoveAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = CreateDockerProcess($"rm --force {containerId}"))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }
        }

        public async Task<IPAddress> ContainerGetDefaultNetworkIpAddressAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = CreateDockerProcess($"inspect {containerId} --format \"{{{{range .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" .IPAddress}}}}{{{{end}}}}\""))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));

                IPAddress.TryParse(process.StandardOutput.ReadLine(), out var expectedIpAddress);

                return expectedIpAddress;
            }
        }

        public async Task ContainerAddFileAsync(string containerId, string hostFilename, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
        {
            using (var process = CreateDockerProcess($"cp \"{hostFilename}\" \"{containerId}:{containerFilename}\""))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                using (var process = CreateDockerProcess($"exec {containerId} chown {owner} \"{containerFilename}\""))
                {
                    await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
                }
            }

            if (!string.IsNullOrWhiteSpace(permissions))
            {
                using (var process = CreateDockerProcess($"exec {containerId} chmod {permissions} \"{containerFilename}\""))
                {
                    await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
                }
            }
        }

        public async Task ContainerRemoveFileAsync(string containerId, string containerFilename, CancellationToken cancellationToken)
        {
            using (var process = CreateDockerProcess($"exec {containerId} rm \"{containerFilename}\""))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }
        }

        public async Task<IEnumerable<string>> ContainerExecuteCommandAsync(string containerId, string command, CancellationToken cancellationToken)
        {
            using (var process = CreateDockerProcess($"exec {containerId} {command}"))
            {
                await RunProcessAsync(process, cancellationToken);

                var lines = new List<string>();

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    lines.Add(line);
                }

                return lines;
            }
        }

        public async Task<int> ContainerGetHostPortMappingAsync(string containerId, string protocol, int containerPort, CancellationToken cancellationToken)
        {
            using (var process = CreateDockerProcess($"port {containerId} {containerPort}/{protocol}"))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));

                int.TryParse((await process.StandardOutput.ReadLineAsync()).Split(':').Last(), out var port);

                return port;
            }
        }

        public async Task<HealthStatus> ContainerGetHealthStatusAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = CreateDockerProcess($"inspect {containerId} -f \"{{{{if .State.Health}}}}{{{{.State.Health.Status}}}}{{{{else}}}}{{{{.State.Status}}}}{{{{end}}}}\""))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));

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

        public async Task NetworkCreateAsync(string networkName, IEnumerable<KeyValuePair<string, string>> options, CancellationToken cancellationToken)
        {
            var formattedOptions = options.Select(x => $"{x.Key} \"{x.Value}\"");

            using (var process = CreateDockerProcess($"network create {string.Join(" ", formattedOptions)} {networkName}"))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }
        }

        public async Task NetworkRemoveAsync(string networkName, CancellationToken cancellationToken)
        {
            using (var process = CreateDockerProcess($"network rm {networkName}"))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }
        }

        public async Task NetworkConnectAsync(string networkName, string containerId, string alias, CancellationToken cancellationToken)
        {
            using (var process = CreateDockerProcess($"network connect --alias {alias} {networkName} {containerId}"))
            {
                await RunProcessAsync(process, cancellationToken.CreateLinkedTimeoutToken(_defaultTimeout));
            }
        }

        private static Process CreateDockerProcess(string arguments)
        {
            var process = new Process();

            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.EnableRaisingEvents = true;

            return process;
        }

        private Task<int> RunProcessAsync(Process process, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            process.Exited += (s, a) =>
            {
                taskCompletionSource.SetResult(process.ExitCode);
            };

            process.Start();

            cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken));

            return taskCompletionSource.Task;
        }
    }
}
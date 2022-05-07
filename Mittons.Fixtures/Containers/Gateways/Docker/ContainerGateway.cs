using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Gateways.Docker
{
    internal class ProcessDebugger
    {
        public Stack<(string Arguments, string StandardOutput, string StandardError)> CallLog { get; } = new Stack<(string Arguments, string StandardOutput, string StandardError)>();

        public void AddLog(string arguments, string standardOutput, string standardError)
        {
            CallLog.Push((arguments, standardOutput, standardError));
        }
    }

    internal class ContainerGateway : IContainerGateway
    {
        private readonly ProcessDebugger _processDebugger;

        public ContainerGateway(ProcessDebugger processDebugger = default)
        {
            _processDebugger = processDebugger;
        }

        public async Task<string> CreateContainerAsync(string imageName, PullOption pullOption, Dictionary<string, string> labels, string command, IHealthCheckDescription healthCheckDescription, CancellationToken cancellationToken)
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

            var arguments = $"run -d --pull {pullOption.ToString().ToLower()} -P {labelOptions} {healthCheck} {imageName} {command}";

            using (var process = new DockerProcess(arguments))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var standardOutput = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                var standardError = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

                _processDebugger?.AddLog(arguments, standardOutput, standardError);

                var containerId = Regex.Split(standardOutput, "[\r\n]+").First();

                return containerId;
            }
        }

        private class HealthCheckReport
        {
            public long Interval { get; set; }

            public long StartPeriod { get; set; }

            public byte Retries { get; set; }
        }

        private async Task<TimeSpan?> GetMinimumHealthCheckTimeSpanAsync(string containerId, CancellationToken cancellationToken)
        {
            long nanosecondModifier = 1000000000;

            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{json .Config.Healthcheck}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var healthCheck = JsonSerializer.Deserialize<HealthCheckReport>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

                if (!(healthCheck is null))
                {
                    return TimeSpan.FromSeconds((healthCheck.StartPeriod / nanosecondModifier) + ((healthCheck.Interval / nanosecondModifier) * healthCheck.Retries));
                }

                return default(TimeSpan?);
            }
        }

        private async Task<string> GetContainerStatusAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{json .State.Status}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    return JsonSerializer.Deserialize<string>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        private async Task<string> GetHealthStatusAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{json .State.Health.Status}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    return JsonSerializer.Deserialize<string>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public async Task EnsureContainerIsHealthyAsync(string containerId, CancellationToken cancellationToken)
        {
            var healthCheckTimeLimit = await GetMinimumHealthCheckTimeSpanAsync(containerId, cancellationToken);

            var timeoutCancellationTokenSource = new CancellationTokenSource();
            timeoutCancellationTokenSource.CancelAfter(healthCheckTimeLimit ?? TimeSpan.FromSeconds(5));

            var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

            while (!linkedCancellationToken.IsCancellationRequested)
            {
                if (healthCheckTimeLimit.HasValue)
                {
                    var status = await GetHealthStatusAsync(containerId, cancellationToken);

                    switch (status)
                    {
                        case "healthy":
                            return;
                        default:
                            break;
                    }
                }
                else
                {
                    var status = await GetContainerStatusAsync(containerId, cancellationToken);

                    switch (status)
                    {
                        case "running":
                            return;
                        default:
                            break;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100));

                linkedCancellationToken.Token.ThrowIfCancellationRequested();
            }
        }

        public async Task<IEnumerable<IResource>> GetAvailableResourcesAsync(string containerId, CancellationToken cancellationToken)
        {
            var portResources = await GetPortResourcesAsync(containerId, cancellationToken).ConfigureAwait(false);
            var volumeResources = await GetVolumeResources(containerId, cancellationToken).ConfigureAwait(false);

            return portResources.Concat(volumeResources).ToArray();
        }

        private async Task<IEnumerable<Resource>> GetPortResourcesAsync(string containerId, CancellationToken cancellationToken)
        {
            var ipAddress = await GetServiceIpAddress(containerId, cancellationToken).ConfigureAwait(false);

            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{json .NetworkSettings.Ports}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                var ports = JsonSerializer.Deserialize<Dictionary<string, Port[]>>(output) ?? new Dictionary<string, Port[]>();
                var hostHostname = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : ipAddress;

                return ports.Where(x => !(x.Value is null)).Select(x =>
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
                        }
                    );
            }
        }

        private async Task<IEnumerable<Resource>> GetVolumeResources(string containerId, CancellationToken cancellationToken)
        {
            var volumes = await GetContainerVolumesAsync(containerId, cancellationToken).ConfigureAwait(false);
            var volumeTasks = volumes.Select(x => GetResourceForVolumeAsync(containerId, x.Destination, cancellationToken)).ToArray();

            await Task.WhenAll(volumeTasks).ConfigureAwait(false);

            return volumeTasks.Select(x => x.Result);
        }

        private async Task<Resource> GetResourceForVolumeAsync(string containerId, string destination, CancellationToken cancellationToken)
        {
            var absolutePath = new Regex(@"^([\/\.])*(.*[^\/])[\/]?$").Replace(destination, "/$2");

            using (var process = new DockerProcess($"exec --workdir / {containerId} stat -c \"%F\" \"{destination}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var output = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

                if (output.Equals("directory"))
                {
                    absolutePath += "/";
                }
            }

            return new Resource(
                    new Uri($"file://{absolutePath}"),
                    new Uri($"file://container.{containerId}{absolutePath}")
                );
        }

        private async Task<Volume[]> GetContainerVolumesAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{json .Mounts}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);

                return JsonSerializer.Deserialize<Volume[]>(output) ?? new Volume[0];
            }
        }

        public async Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"rm -v --force {containerId}"))
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

        public Task SetFileSystemResourceOwnerAsync(string containerId, string path, string owner, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetFileSystemResourcePermissionsAsync(string containerId, string path, string permissions, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CreateFileAsync(string containerId, string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteFileAsync(string containerId, string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AppendFileAsync(string containerId, string path, string contents, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task WriteFileAsync(string containerId, string path, string contents, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> ReadFileAsync(string containerId, string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private class Volume
        {
            public string Destination { get; set; }
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

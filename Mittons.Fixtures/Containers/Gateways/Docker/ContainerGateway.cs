using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Containers.Resources;
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

        public async Task SetFileSystemResourceOwnerAsync(string containerId, string path, string owner, CancellationToken cancellationToken)
        {
            if (!await DoesFileSystemResourceExist(containerId, path, cancellationToken))
            {
                throw new InvalidOperationException($"Resource [{path}] does not exist.");
            }

            using (var process = new DockerProcess($"exec {containerId} chown {owner} \"{path}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SetFileSystemResourcePermissionsAsync(string containerId, string path, string permissions, CancellationToken cancellationToken)
        {
            if (!await DoesFileSystemResourceExist(containerId, path, cancellationToken))
            {
                throw new InvalidOperationException($"Resource [{path}] does not exist.");
            }

            throw new NotImplementedException();
        }

        private async Task<bool> DoesFileSystemResourceExist(string containerId, string path, CancellationToken cancellationToken)
        {
            var existsExitCode = 0;

            using (var process = new DockerProcess($"exec {containerId} ls \"{path}\""))
            {
                return await process.RunProcessAsync(cancellationToken).ConfigureAwait(false) == existsExitCode;
            }
        }

        private async Task EnsureDirectoryExists(string containerId, string path, CancellationToken cancellationToken)
        {
            var directoryPath = Path.GetDirectoryName(path).Replace("\\", "/");

            if (!await DoesFileSystemResourceExist(containerId, directoryPath, cancellationToken))
            {
                using (var process = new DockerProcess($"exec {containerId} mkdir -p \"{directoryPath}\""))
                {
                    await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task CreateFileAsync(string containerId, string path, CancellationToken cancellationToken)
        {
            if (await DoesFileSystemResourceExist(containerId, path, cancellationToken))
            {
                throw new InvalidOperationException($"File [{path}] already exists.");
            }

            await EnsureDirectoryExists(containerId, path, cancellationToken).ConfigureAwait(false);

            using (var process = new DockerProcess($"exec {containerId} touch \"{path}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task DeleteFileAsync(string containerId, string path, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"exec {containerId} rm \"{path}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task AppendFileAsync(string containerId, string path, string contents, CancellationToken cancellationToken)
        {
            var originalContents = await ReadFileAsync(containerId, path, cancellationToken);

            await WriteFileAsync(containerId, path, $"{originalContents}{contents}", cancellationToken);
        }

        public async Task WriteFileAsync(string containerId, string path, string contents, CancellationToken cancellationToken)
        {
            var localPath = Path.GetTempFileName();

            File.WriteAllText(localPath, contents);

            await EnsureDirectoryExists(containerId, path, cancellationToken).ConfigureAwait(false);

            using (var process = new DockerProcess($"cp \"{localPath}\" \"{containerId}:{path}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Delete(localPath);
        }

        public async Task<string> ReadFileAsync(string containerId, string path, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"exec {containerId} cat {path}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                return await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        public async Task CreateDirectoryAsync(string containerId, string path, CancellationToken cancellationToken)
        {
            if (await DoesFileSystemResourceExist(containerId, path, cancellationToken))
            {
                throw new InvalidOperationException($"Directory [{path}] already exists.");
            }

            using (var process = new DockerProcess($"exec {containerId} mkdir -p \"{path}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task DeleteDirectoryAsync(string containerId, string path, bool recursive, CancellationToken cancellationToken)
        {
            if (recursive)
            {
                using (var process = new DockerProcess($"exec {containerId} rm -rf \"{path}\""))
                {
                    await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                using (var process = new DockerProcess($"exec {containerId} ls \"{path}\""))
                {
                    await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false)))
                    {
                        throw new InvalidOperationException($"Directory [{path}] has children, but recursive delete was not requested.");
                    }
                }

                using (var process = new DockerProcess($"exec {containerId} rmdir \"{path}\""))
                {
                    await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> IsDirectory(string containerId, string path, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"exec --workdir / {containerId} stat -c \"%F\" \"{path}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var output = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

                return output.Equals("directory");
            }
        }

        private async Task<IEnumerable<IFileSystemResourceAdapter>> Enumerate(string containerId, string path, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"exec {containerId} ls -1 \"{path}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var adapters = new List<IFileSystemResourceAdapter>();

                while (!process.StandardOutput.EndOfStream)
                {
                    var childPath = $"{path}/{await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)}";

                    if (await DoesFileSystemResourceExist(containerId, childPath, cancellationToken))
                    {
                        var isDirectory = await IsDirectory(containerId, childPath, cancellationToken).ConfigureAwait(false);

                        if (isDirectory)
                        {
                            adapters.Add(new DirectoryResourceAdapter(containerId, childPath, this));
                        }
                        else
                        {
                            adapters.Add(new FileResourceAdapter(containerId, childPath, this));
                        }
                    }
                }

                return adapters;
            }
        }

        public async Task<IEnumerable<IDirectoryResourceAdapter>> EnumerateDirectoriesAsync(string containerId, string path, CancellationToken cancellationToken)
        {
            var children = await Enumerate(containerId, path, cancellationToken);

            return children.OfType<DirectoryResourceAdapter>();
        }

        public async Task<IEnumerable<IFileResourceAdapter>> EnumerateFilesAsync(string containerId, string path, CancellationToken cancellationToken)
        {
            var children = await Enumerate(containerId, path, cancellationToken);

            return children.OfType<FileResourceAdapter>();
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

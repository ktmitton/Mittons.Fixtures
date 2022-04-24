using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Attributes;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Exceptions.Containers;
using Mittons.Fixtures.Extensions;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Containers.Gateways
{
    public class DockerServiceGateway : IServiceGateway<IContainerService>
    {
        /// <inheritdoc/>
        /// <exception cref="Mittons.Fixtures.Exceptions.Containers.ImageNameMissingException">Thrown when no <see cref="Mittons.Fixtures.Docker.Attributes.ImageAttribute"/> has been provided.</exception>
        /// <exception cref="Mittons.Fixtures.Exceptions.Containers.MultipleImageNamesProvidedException">Thrown when multiple <see cref="Mittons.Fixtures.Docker.Attributes.ImageAttribute"/> have been provided.</exception>
        public async Task<IContainerService> CreateServiceAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
        {
            var image = attributes.OfType<ImageAttribute>().ToArray();

            if (image.Length == 0)
            {
                throw new ImageNameMissingException();
            }
            else if (image.Length > 1)
            {
                throw new MultipleImageNamesProvidedException();
            }

            var command = string.Join(" ", attributes.OfType<CommandAttribute>().Select(x => x.Value));

            var options = new List<Option>();

            var healthCheck = attributes.OfType<HealthCheckAttribute>().FirstOrDefault();

            if (!(healthCheck is null))
            {
                if (healthCheck.Disabled)
                {
                    options.Add(new Option { Name = "--no-healthcheck", Value = string.Empty });
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(healthCheck.Command))
                    {
                        options.Add(new Option { Name = "--health-cmd", Value = healthCheck.Command });
                    }

                    if (healthCheck.Interval > 0)
                    {
                        options.Add(new Option { Name = "--health-interval", Value = $"{healthCheck.Interval}s" });
                    }

                    if (healthCheck.Timeout > 0)
                    {
                        options.Add(new Option { Name = "--health-timeout", Value = $"{healthCheck.Timeout}s" });
                    }

                    if (healthCheck.StartPeriod > 0)
                    {
                        options.Add(new Option { Name = "--health-start-period", Value = $"{healthCheck.StartPeriod}s" });
                    }

                    if (healthCheck.Retries > 0)
                    {
                        options.Add(new Option { Name = "--health-retries", Value = healthCheck.Retries.ToString() });
                    }
                }
            }

            var run = attributes.OfType<RunAttribute>().Single();

            options.Add(new Option { Name = "--label", Value = $"mittons.fixtures.run.id={run.Id}" });

            using (var process = new DockerProcess($"run -d -P {options.ToExecutionParametersFormattedString()} {image.First().Name} {command}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var containerId = process.StandardOutput.ReadLine();

                return new ContainerService(containerId, await GetServiceResourcesAsync(containerId, cancellationToken).ConfigureAwait(false));
            }
        }

        /// <inheritdoc/>
        public async Task RemoveServiceAsync(IContainerService service, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"rm --force {service.ServiceId}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<string> GetServiceIpAddress(string containerId, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"inspect {containerId} --format \"{{{{.NetworkSettings.IPAddress}}}}\""))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                return await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<IResource>> GetServiceResourcesAsync(string containerId, CancellationToken cancellationToken)
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
                    var temp1 = new Uri($"{x.Key.Split('/').Last()}://127.0.0.1:{x.Key.Split('/').First()}");
                    var temp2 = new Uri($"{x.Key.Split('/').Last()}://{publicHost}:{publicPort}");

                    return new Resource(
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
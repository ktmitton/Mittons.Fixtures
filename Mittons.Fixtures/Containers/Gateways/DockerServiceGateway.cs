using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Exceptions.Containers;

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

            using (var process = new DockerProcess($"run -d -P {image.First().Name} {command}"))
            {
                await process.RunProcessAsync(cancellationToken).ConfigureAwait(false);

                var containerId = process.StandardOutput.ReadLine();

                return new ContainerService(containerId, await GetServiceResourcesAsync(containerId, cancellationToken));
            }
        }

        /// <inheritdoc/>
        public async Task RemoveServiceAsync(IContainerService service, CancellationToken cancellationToken)
        {
            using (var process = new DockerProcess($"rm --force {service.ContainerId}"))
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
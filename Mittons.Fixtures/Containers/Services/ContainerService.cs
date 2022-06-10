using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Services
{
    public class ContainerService : IContainerService
    {
        public IEnumerable<IResource> Resources { get; private set; }

        public string ServiceId { get; private set; }

        private readonly IContainerGateway _containerGateway;

        private bool _teardownOnDispose;

        public ContainerService(IContainerGateway containerGateway)
        {
            _containerGateway = containerGateway;
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(_teardownOnDispose ? _containerGateway.RemoveContainerAsync(ServiceId, CancellationToken.None) : Task.CompletedTask);
        }

        public async Task InitializeAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
        {
            var run = attributes.OfType<RunAttribute>().Single();

            _teardownOnDispose = run.TeardownOnComplete;

            var image = attributes.OfType<ImageAttribute>().Single();

            var build = attributes.OfType<BuildAttribute>().SingleOrDefault();

            if (!(build is null))
            {
                await _containerGateway.BuildImageAsync(
                        build.DockerfilePath,
                        build.Target,
                        build.PullDependencyImages,
                        image.Name,
                        build.Context,
                        build.Arguments,
                        cancellationToken
                    ).ConfigureAwait(false);
            }

            var command = attributes.OfType<CommandAttribute>().SingleOrDefault();

            var healthCheckDescription = attributes.OfType<IHealthCheckDescription>().SingleOrDefault();

            var environmentVariables = attributes.OfType<EnvironmentVariableAttribute>().ToDictionary(x => x.Key, x => x.Value);

            var hostname = attributes.OfType<HostnameAttribute>().SingleOrDefault();

            ServiceId = await _containerGateway.CreateContainerAsync(
                    image.Name,
                    image.PullOption,
                    new Dictionary<string, string>
                    {
                        { "mittons.fixtures.run.id", run.Id }
                    },
                    environmentVariables,
                    hostname?.Name,
                    command?.Value,
                    healthCheckDescription,
                    cancellationToken
                ).ConfigureAwait(false);

            await _containerGateway.EnsureContainerIsHealthyAsync(ServiceId, cancellationToken).ConfigureAwait(false);

            Resources = await _containerGateway.GetAvailableResourcesAsync(ServiceId, cancellationToken).ConfigureAwait(false);

            var networkAliases = attributes.OfType<NetworkAliasAttribute>();

            foreach (var alias in networkAliases)
            {
                await alias.NetworkService.ConnectAsync(alias, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

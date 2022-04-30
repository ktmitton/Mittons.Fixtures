using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Attributes;
using Mittons.Fixtures.Containers.Gateways;

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
            return new ValueTask(_teardownOnDispose ? _containerGateway.RemoveContainerAsync(default, default) : Task.CompletedTask);
        }

        public async Task InitializeAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
        {
            var run = attributes.OfType<RunAttribute>().Single();

            _teardownOnDispose = run.TeardownOnComplete;

            ServiceId = await _containerGateway.CreateContainerAsync(
                    new Dictionary<string, string>
                    {
                        { "mittons.fixtures.run.id", run.Id }
                    },
                    cancellationToken
                );

            Resources = Enumerable.Empty<IResource>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;
using Xunit;

namespace Mittons.Fixtures.Docker.Containers
{
    public class Container : IAsyncLifetime
    {
        public string Id { get; private set; }

        public IPAddress IpAddress { get; private set; }

        protected readonly IDockerGateway _dockerGateway;

        private readonly string _imageName;

        private readonly string _command;

        private readonly Guid _instanceId;

        private readonly Dictionary<string, string> _labels;

        private readonly IEnumerable<NetworkAlias> _networks;

        public Container(IDockerGateway dockerGateway, Guid instanceId, IEnumerable<Attribute> attributes)
        {
            _dockerGateway = dockerGateway;

            _instanceId = instanceId;

            _imageName = attributes.OfType<Image>().Single().Name;

            _command = attributes.OfType<Command>().SingleOrDefault()?.Value ?? string.Empty;

            _labels = (attributes.OfType<Run>().SingleOrDefault() ?? new Run()).Labels;

            _networks = attributes.OfType<NetworkAlias>();
        }

        public virtual async Task InitializeAsync()
        {
            Id = await _dockerGateway.ContainerRunAsync(_imageName, _command, _labels, CancellationToken.None);
            IpAddress = await _dockerGateway.ContainerGetDefaultNetworkIpAddressAsync(Id, CancellationToken.None);

            foreach (var networkAlias in _networks)
            {
                await _dockerGateway.NetworkConnectAsync($"{networkAlias.NetworkName}-{_instanceId}", Id, networkAlias.Alias, CancellationToken.None);
            }
        }

        public virtual async Task DisposeAsync()
        {
            await _dockerGateway.ContainerRemoveAsync(Id, CancellationToken.None);
        }

        public async Task CreateFileAsync(string fileContents, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
        {
            var temporaryFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            File.WriteAllText(temporaryFilename, fileContents);

            await AddFileAsync(temporaryFilename, containerFilename, owner, permissions, cancellationToken);

            File.Delete(temporaryFilename);
        }

        public async Task CreateFileAsync(Stream fileContents, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
        {
            var temporaryFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            using (var fileStream = new FileStream(temporaryFilename, FileMode.Create, FileAccess.Write))
            {
                fileContents.CopyTo(fileStream);
            }

            await AddFileAsync(temporaryFilename, containerFilename, owner, permissions, cancellationToken);

            File.Delete(temporaryFilename);
        }

        public Task AddFileAsync(string hostFilename, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
            => _dockerGateway.ContainerAddFileAsync(Id, hostFilename, containerFilename, owner, permissions, cancellationToken);

        public Task RemoveFileAsync(string containerFilename, CancellationToken cancellationToken)
            => _dockerGateway.ContainerRemoveFileAsync(Id, containerFilename, cancellationToken);

        public virtual async Task<string> StartAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();
            timeoutCancellationTokenSource.CancelAfter(timeout);

            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token).Token;

            while (!linkedToken.IsCancellationRequested)
            {
                var healthStatus = await _dockerGateway.ContainerGetHealthStatusAsync(Id, linkedToken);

                if ((healthStatus == HealthStatus.Running) || (healthStatus == HealthStatus.Healthy))
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            linkedToken.ThrowIfCancellationRequested();

            return Id;
        }
    }
}
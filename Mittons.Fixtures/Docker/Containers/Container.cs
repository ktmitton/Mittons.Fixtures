using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Containers
{
    public class Container : IAsyncLifetime
    {
        public string Id { get; private set; }

        public IPAddress IpAddress { get; private set; }

        protected readonly IContainerGateway _containerGateway;

        protected readonly INetworkGateway _networkGateway;

        private readonly string _imageName;

        private readonly string _command;

        private readonly Guid _instanceId;

        private readonly IEnumerable<Option> _options;

        private readonly IEnumerable<NetworkAliasAttribute> _networks;

        private readonly bool _teardownOnComplete;

        public Container(IContainerGateway containerGateway, INetworkGateway networkGateway, Guid instanceId, IEnumerable<Attribute> attributes)
        {
            _containerGateway = containerGateway;

            _networkGateway = networkGateway;

            _instanceId = instanceId;

            _imageName = attributes.OfType<ImageAttribute>().Single().Name;

            _command = attributes.OfType<CommandAttribute>().SingleOrDefault()?.Value ?? string.Empty;

            _options = attributes.OfType<IOptionAttribute>().SelectMany(x => x.Options).ToArray();

            _networks = attributes.OfType<NetworkAliasAttribute>();

            _teardownOnComplete = attributes.OfType<RunAttribute>().SingleOrDefault()?.TeardownOnComplete ?? true;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked after an instance of <see cref="Container"/> is created, before it is used.
        /// </remarks>
        public virtual async Task InitializeAsync(CancellationToken cancellationToken)
        {
            Id = await _containerGateway.RunAsync(_imageName, _command, _options, cancellationToken);
            IpAddress = await _containerGateway.GetDefaultNetworkIpAddressAsync(Id, cancellationToken);

            await EnsureHealthyAsync(TimeSpan.FromSeconds(5));

            foreach (var networkAlias in _networks)
            {
                await _networkGateway.ConnectAsync($"{networkAlias.NetworkName}-{_instanceId}", Id, networkAlias.Alias, cancellationToken);
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This must be invoked when an instance of <see cref="Container"/> is no longer used.
        /// </remarks>
        public virtual Task DisposeAsync()
            => _teardownOnComplete ? _containerGateway.RemoveAsync(Id, CancellationToken.None) : Task.CompletedTask;

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
            => _containerGateway.AddFileAsync(Id, hostFilename, containerFilename, owner, permissions, cancellationToken);

        public Task RemoveFileAsync(string containerFilename, CancellationToken cancellationToken)
            => _containerGateway.RemoveFileAsync(Id, containerFilename, cancellationToken);

        private async Task<string> EnsureHealthyAsync(TimeSpan timeout)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();
            timeoutCancellationTokenSource.CancelAfter(timeout);

            while (!timeoutCancellationTokenSource.Token.IsCancellationRequested)
            {
                var healthStatus = await _containerGateway.GetHealthStatusAsync(Id, timeoutCancellationTokenSource.Token);

                if ((healthStatus == HealthStatus.Running) || (healthStatus == HealthStatus.Healthy))
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            timeoutCancellationTokenSource.Token.ThrowIfCancellationRequested();

            return Id;
        }
    }
}
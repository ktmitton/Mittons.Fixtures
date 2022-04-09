using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Containers
{
    public class Container : IDisposable, IServiceLifetime
    {
        public string Id { get; }

        public IPAddress IpAddress { get; set; }

        private readonly IDockerGateway _dockerGateway;

        public Container(IDockerGateway dockerGateway, IEnumerable<Attribute> attributes)
        {
            _dockerGateway = dockerGateway;

            var run = attributes.OfType<Run>().SingleOrDefault() ?? new Run();

            Id = _dockerGateway.ContainerRunAsync(
                    attributes.OfType<Image>().Single().Name,
                    attributes.OfType<Command>().SingleOrDefault()?.Value ?? string.Empty,
                    (attributes.OfType<Run>().SingleOrDefault() ?? new Run()).Labels,
                    CancellationToken.None
                ).GetAwaiter().GetResult();
            IpAddress = _dockerGateway.ContainerGetDefaultNetworkIpAddressAsync(Id, CancellationToken.None).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _dockerGateway.ContainerRemoveAsync(Id, CancellationToken.None).GetAwaiter().GetResult();
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

        public virtual Task<HealthStatus> GetHealthStatusAsync()
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Containers
{
    public class Container : IDisposable
    {
        public string Id { get; }

        public IPAddress IpAddress { get; set; }

        private readonly IDockerGateway _dockerGateway;

        public Container(IDockerGateway dockerGateway, IEnumerable<Attribute> attributes)
        {
            _dockerGateway = dockerGateway;

            var run = attributes.OfType<Run>().SingleOrDefault() ?? new Run();

            Id = _dockerGateway.ContainerRun(
                    attributes.OfType<Image>().Single().Name,
                    attributes.OfType<Command>().SingleOrDefault()?.Value ?? string.Empty,
                    (attributes.OfType<Run>().SingleOrDefault() ?? new Run()).Labels
                );
            IpAddress = _dockerGateway.ContainerGetDefaultNetworkIpAddress(Id);
        }

        public void Dispose()
        {
            _dockerGateway.ContainerRemove(Id);
        }

        public void CreateFile(string fileContents, string containerFilename, string owner, string permissions)
        {
            var temporaryFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            File.WriteAllText(temporaryFilename, fileContents);

            AddFile(temporaryFilename, containerFilename, owner, permissions);

            File.Delete(temporaryFilename);
        }

        public void CreateFile(Stream fileContents, string containerFilename, string owner, string permissions)
        {
            var temporaryFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            using (var fileStream = new FileStream(temporaryFilename, FileMode.Create, FileAccess.Write))
            {
                fileContents.CopyTo(fileStream);
            }

            AddFile(temporaryFilename, containerFilename, owner, permissions);

            File.Delete(temporaryFilename);
        }

        public void AddFile(string hostFilename, string containerFilename, string owner, string permissions)
        {
            _dockerGateway.ContainerAddFile(Id, hostFilename, containerFilename, owner, permissions);
        }

        public void RemoveFile(string containerFilename)
        {
            _dockerGateway.ContainerRemoveFile(Id, containerFilename);
        }
    }
}
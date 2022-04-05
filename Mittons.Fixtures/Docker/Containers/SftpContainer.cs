using System;
using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;

namespace Mittons.Fixtures.Docker.Containers
{
    public class SftpContainer : Container
    {
        private const string ImageName = "atmoz/sftp:alpine";

        public SftpContainer(IDockerGateway dockerGateway, IEnumerable<Attribute> attributes)
            : base(dockerGateway, attributes.Concat(new Attribute[] { new Image(ImageName), new Command(BuildCommand(attributes.OfType<SftpUserAccount>()))}))
        {
        }

        private static string BuildCommand(IEnumerable<SftpUserAccount> userAccounts)
            => string.Join(" ", ((userAccounts?.Any() ?? false) ? userAccounts : new SftpUserAccount[] { new SftpUserAccount("guest", "guest") }).Select(x => $"{x.Username}:{x.Password}"));
    }
}
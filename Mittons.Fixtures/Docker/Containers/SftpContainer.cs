using System;
using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Containers
{
    public class SftpContainer : Container
    {
        private const string ImageName = "atmoz/sftp";

        public SftpContainer(IDockerGateway dockerGateway, IEnumerable<Attribute> attributes)
            : base(dockerGateway, ImageName, BuildCommand(attributes.OfType<SftpUserAccount>()))
        {
        }

        public SftpContainer(IDockerGateway dockerGateway, IEnumerable<SftpUserAccount> userAccounts)
            : base(dockerGateway, ImageName, BuildCommand(userAccounts))
        {
        }

        private static string BuildCommand(IEnumerable<SftpUserAccount> userAccounts)
            => string.Join(" ", ((userAccounts?.Any() ?? false) ? userAccounts : new SftpUserAccount[] { new SftpUserAccount("guest", "guest") }).Select(x => $"{x.Username}:{x.Password}"));
    }
}
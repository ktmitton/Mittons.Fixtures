using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Containers
{
    public class SftpContainer : Container
    {
        private const string ImageName = "atmoz/sftp:alpine";

        public Dictionary<string, SftpConnectionSettings> SftpConnectionSettings { get; }

        public SftpContainer(IDockerGateway dockerGateway, IEnumerable<Attribute> attributes)
            : base(dockerGateway, attributes.Concat(new Attribute[] { new Image(ImageName), new Command(BuildCommand(ExtractSftpUserAccounts(attributes))) }))
        {
            var accounts = ExtractSftpUserAccounts(attributes);

            var host = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : IpAddress.ToString();
            var port = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? dockerGateway.ContainerGetHostPortMappingAsync(Id, "tcp", 22, CancellationToken.None).GetAwaiter().GetResult() : 22;
            var rsaFingerprint = new Fingerprint { Md5 = GetFingerprint(dockerGateway, "rsa", "md5"), Sha256 = GetFingerprint(dockerGateway, "rsa", "sha256") };
            var ed25519Fingerprint = new Fingerprint { Md5 = GetFingerprint(dockerGateway, "ed25519", "md5"), Sha256 = GetFingerprint(dockerGateway, "ed25519", "sha256") };

            SftpConnectionSettings = accounts.Select(
                    x =>
                    new KeyValuePair<string, SftpConnectionSettings>(
                        x.Username,
                        new SftpConnectionSettings
                        {
                            Host = host,
                            Port = port,
                            Username = x.Username,
                            Password = x.Password,
                            RsaFingerprint = rsaFingerprint,
                            Ed25519Fingerprint = ed25519Fingerprint
                        }
                    )
                ).ToDictionary(x => x.Key, x => x.Value);
        }

        private string GetFingerprint(IDockerGateway dockerGateway, string algorithm, string hash)
        {
            var execResults = dockerGateway.ContainerExecuteCommand(Id, $"ssh-keygen -l -E {hash} -f /etc/ssh/ssh_host_{algorithm}_key.pub").ToArray();

            if (execResults.Length != 1)
            {
                return string.Empty;
            }

            var parts = execResults.Single().Split(new[] { ' ' }, 3);

            if (parts.Length != 3)
            {
                return string.Empty;
            }

            var fingerprint = parts[1].Split(new[] { ':' }, 2);

            if (fingerprint.Length != 2)
            {
                return string.Empty;
            }

            return fingerprint[1];
        }

        private static IEnumerable<SftpUserAccount> ExtractSftpUserAccounts(IEnumerable<Attribute> attributes)
        {
            var accounts = attributes.OfType<SftpUserAccount>();

            if (accounts.Any())
            {
                return accounts;
            }

            return new[] { new SftpUserAccount("guest", "guest") };
        }

        private static string BuildCommand(IEnumerable<SftpUserAccount> userAccounts)
            => string.Join(" ", userAccounts.Select(x => $"{x.Username}:{x.Password}"));
    }
}
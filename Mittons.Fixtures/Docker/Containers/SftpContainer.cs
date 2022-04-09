using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Extensions;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Docker.Containers
{
    public class SftpContainer : Container
    {
        private const string ImageName = "atmoz/sftp:alpine";

        public Dictionary<string, SftpConnectionSettings> SftpConnectionSettings { get; private set; }

        private IEnumerable<SftpUserAccount> _accounts;

        public SftpContainer(IDockerGateway dockerGateway, IEnumerable<Attribute> attributes)
            : base(dockerGateway, attributes.Concat(new Attribute[] { new Image(ImageName), new Command(BuildCommand(ExtractSftpUserAccounts(attributes))) }))
        {
            _accounts = ExtractSftpUserAccounts(attributes);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var host = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : IpAddress.ToString();
            var port = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? await _dockerGateway.ContainerGetHostPortMappingAsync(Id, "tcp", 22, CancellationToken.None) : 22;
            var rsaFingerprint = new Fingerprint
                {
                    Md5 = await GetFingerprintAsync(_dockerGateway, "rsa", "md5", CancellationToken.None),
                    Sha256 = await GetFingerprintAsync(_dockerGateway, "rsa", "sha256", CancellationToken.None)
                };
            var ed25519Fingerprint = new Fingerprint
                {
                    Md5 = await GetFingerprintAsync(_dockerGateway, "ed25519", "md5", CancellationToken.None),
                    Sha256 = await GetFingerprintAsync(_dockerGateway, "ed25519", "sha256", CancellationToken.None)
                };

            SftpConnectionSettings = _accounts.Select(
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

        private async Task<string> GetFingerprintAsync(IDockerGateway dockerGateway, string algorithm, string hash, CancellationToken cancellationToken)
        {
            var execResults = (await dockerGateway.ContainerExecuteCommandAsync(Id, $"ssh-keygen -l -E {hash} -f /etc/ssh/ssh_host_{algorithm}_key.pub", cancellationToken.CreateLinkedTimeoutToken(TimeSpan.FromSeconds(5)))).ToArray();

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
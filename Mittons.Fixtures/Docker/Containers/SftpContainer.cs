using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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

        public SftpContainer(IDockerGateway dockerGateway, Guid instanceId, IEnumerable<Attribute> attributes)
            : base(dockerGateway, instanceId, GetAttributesWithDefaults(attributes))
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

        public Task CreateFileAsync(Stream fileContents, string user, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
            => CreateFileAsync(fileContents, Path.Combine($"/home/{user}", Regex.Replace(containerFilename, "^/", "")).Replace("\\", "/"), owner, permissions, cancellationToken);

        public Task CreateFileAsync(string fileContents, string user, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
            => CreateFileAsync(fileContents, Path.Combine($"/home/{user}", Regex.Replace(containerFilename, "^/", "")).Replace("\\", "/"), owner, permissions, cancellationToken);

        public Task AddFileAsync(string hostFilename, string user, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
            => AddFileAsync(hostFilename, Path.Combine($"/home/{user}", Regex.Replace(containerFilename, "^/", "")).Replace("\\", "/"), owner, permissions, cancellationToken);

        public Task RemoveFileAsync(string user, string containerFilename, CancellationToken cancellationToken)
            => RemoveFileAsync(Path.Combine($"/home/{user}", Regex.Replace(containerFilename, "^/", "")).Replace("\\", "/"), cancellationToken);

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

        private static IEnumerable<Attribute> GetAttributesWithDefaults(IEnumerable<Attribute> attributes)
        {
            var fullAttributes = attributes.Concat(
                    new Attribute[]
                    {
                        new Image(ImageName),
                        new Command(BuildCommand(ExtractSftpUserAccounts(attributes)))
                    }
                );

            return fullAttributes.OfType<HealthCheck>().Any() ?
                fullAttributes :
                fullAttributes.Concat(
                    new Attribute[]
                    {
                        new HealthCheck
                        {
                            Disabled = false,
                            Command = "ps aux | grep -v grep | grep sshd || exit 1",
                            Interval = TimeSpan.FromSeconds(1),
                            Timeout = TimeSpan.FromSeconds(1),
                            StartPeriod = TimeSpan.FromSeconds(5),
                            Retries = 3
                        }
                    }
                );
        }
    }
}
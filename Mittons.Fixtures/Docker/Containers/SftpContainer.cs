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

        private readonly IEnumerable<SftpUserAccountAttribute> _accounts;

        public SftpContainer(IContainerGateway containerGateway, INetworkGateway networkGateway, Guid instanceId, IEnumerable<Attribute> attributes)
            : base(containerGateway, networkGateway, instanceId, GetAttributesWithDefaults(attributes))
        {
            _accounts = ExtractSftpUserAccounts(attributes);
        }

        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await base.InitializeAsync(cancellationToken).ConfigureAwait(false);

            var host = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : IpAddress.ToString();
            var port = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? await _containerGateway.GetHostPortMappingAsync(Id, "tcp", 22, cancellationToken).ConfigureAwait(false) : 22;
            var rsaFingerprint = new Fingerprint
            {
                Md5 = await GetFingerprintAsync(_containerGateway, "rsa", "md5", cancellationToken).ConfigureAwait(false),
                Sha256 = await GetFingerprintAsync(_containerGateway, "rsa", "sha256", cancellationToken).ConfigureAwait(false)
            };
            var ed25519Fingerprint = new Fingerprint
            {
                Md5 = await GetFingerprintAsync(_containerGateway, "ed25519", "md5", cancellationToken).ConfigureAwait(false),
                Sha256 = await GetFingerprintAsync(_containerGateway, "ed25519", "sha256", cancellationToken).ConfigureAwait(false)
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

        public Task CreateUserFileAsync(string user, Stream fileContents, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
            => CreateFileAsync(fileContents, Path.Combine($"/home/{user}", Regex.Replace(containerFilename, "^/", "")).Replace("\\", "/"), owner, permissions, cancellationToken);

        public Task CreateUserFileAsync(string user, string fileContents, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
            => CreateFileAsync(fileContents, Path.Combine($"/home/{user}", Regex.Replace(containerFilename, "^/", "")).Replace("\\", "/"), owner, permissions, cancellationToken);

        public Task AddUserFileAsync(string user, string hostFilename, string containerFilename, string owner, string permissions, CancellationToken cancellationToken)
            => AddFileAsync(hostFilename, Path.Combine($"/home/{user}", Regex.Replace(containerFilename, "^/", "")).Replace("\\", "/"), owner, permissions, cancellationToken);

        public Task RemoveUserFileAsync(string user, string containerFilename, CancellationToken cancellationToken)
            => RemoveFileAsync(Path.Combine($"/home/{user}", Regex.Replace(containerFilename, "^/", "")).Replace("\\", "/"), cancellationToken);

        public Task EmptyUserDirectoryAsync(string user, CancellationToken cancellationToken)
            => EmptyDirectoryAsync($"/home/{user}", cancellationToken);

        private async Task<string> GetFingerprintAsync(IContainerGateway containerGateway, string algorithm, string hash, CancellationToken cancellationToken)
        {
            var execResults = (await containerGateway.ExecuteCommandAsync(Id, $"ssh-keygen -l -E {hash} -f /etc/ssh/ssh_host_{algorithm}_key.pub", cancellationToken.CreateLinkedTimeoutToken(TimeSpan.FromSeconds(5))).ConfigureAwait(false)).ToArray();

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

        private static IEnumerable<SftpUserAccountAttribute> ExtractSftpUserAccounts(IEnumerable<Attribute> attributes)
        {
            var accounts = attributes.OfType<SftpUserAccountAttribute>();

            if (accounts.Any())
            {
                return accounts;
            }

            return new[] { new SftpUserAccountAttribute("guest", "guest") };
        }

        private static string BuildCommand(IEnumerable<SftpUserAccountAttribute> userAccounts)
            => string.Join(" ", userAccounts.Select(x => $"{x.Username}:{x.Password}"));

        private static IEnumerable<Attribute> GetAttributesWithDefaults(IEnumerable<Attribute> attributes)
        {
            var fullAttributes = attributes.Concat(
                    new Attribute[]
                    {
                        new ImageAttribute(ImageName),
                        new CommandAttribute(BuildCommand(ExtractSftpUserAccounts(attributes)))
                    }
                );

            return fullAttributes.OfType<HealthCheckAttribute>().Any() ?
                fullAttributes :
                fullAttributes.Concat(
                    new Attribute[]
                    {
                        new HealthCheckAttribute
                        {
                            Disabled = false,
                            Command = "ps aux | grep -v grep | grep sshd || exit 1",
                            Interval = 1,
                            Timeout = 1,
                            StartPeriod = 5,
                            Retries = 3
                        }
                    }
                );
        }
    }
}
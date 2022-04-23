using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Models;
using Mittons.Fixtures.Resources;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers;

public class SftpContainerTests : BaseContainerTests
{
    public class HealthCheckTests : BaseContainerTests
    {
        [Fact]
        public async Task InitializeAsync_WhenNoHealthCheckIsProvided_ExpectDefaultHealthCheckToBeApplied()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    Enumerable.Empty<Attribute>()
                );
            _containers.Add(container);

            // Act
            await container.InitializeAsync(CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(
                    x =>
                    x.RunAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.Is<IEnumerable<Option>>(x =>
                            !x.Any(y => y.Name == "--no-healthcheck") &&
                            x.Any(y => y.Name == "--health-cmd" && y.Value == "ps aux | grep -v grep | grep sshd || exit 1") &&
                            x.Any(y => y.Name == "--health-interval" && y.Value == "1s") &&
                            x.Any(y => y.Name == "--health-timeout" && y.Value == "1s") &&
                            x.Any(y => y.Name == "--health-start-period" && y.Value == "5s") &&
                            x.Any(y => y.Name == "--health-retries" && y.Value == "3")
                        ),
                        It.IsAny<CancellationToken>()
                    )
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenAHealthCheckIsProvided_ExpectProvidedHealthCheckToBeApplied()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new HealthCheckAttribute
                        {
                            Disabled = false,
                            Command = "test",
                            Interval = 2,
                            Timeout = 2,
                            StartPeriod = 2,
                            Retries = 1
                        }
                    }
                );
            _containers.Add(container);

            // Act
            await container.InitializeAsync(CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(
                    x =>
                    x.RunAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.Is<IEnumerable<Option>>(x =>
                            !x.Any(y => y.Name == "--no-healthcheck") &&
                            x.Any(y => y.Name == "--health-cmd" && y.Value == "test") &&
                            x.Any(y => y.Name == "--health-interval" && y.Value == "2s") &&
                            x.Any(y => y.Name == "--health-timeout" && y.Value == "2s") &&
                            x.Any(y => y.Name == "--health-start-period" && y.Value == "2s") &&
                            x.Any(y => y.Name == "--health-retries" && y.Value == "1")
                        ),
                        It.IsAny<CancellationToken>()
                    )
                );
        }
    }

    public class ImageTests : BaseContainerTests
    {
        private readonly string sftpImageName = "atmoz/sftp:alpine";

        [Fact]
        public async Task InitializeAsync_WhenCalled_ExpectTheContainerToUseTheAtmozImage()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, Enumerable.Empty<Attribute>());
            _containers.Add(container);

            // Act
            await container.InitializeAsync(CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.RunAsync(sftpImageName, It.IsAny<string>(), It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class CredentialsTests : BaseContainerTests
    {
        [Fact]
        public async Task InitializeAsync_InitializedWithNoCredentials_ExpectTheCommandToSetupTheGuestAccount()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, Enumerable.Empty<Attribute>());
            _containers.Add(container);

            // Act
            await container.InitializeAsync(CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.RunAsync(It.IsAny<string>(), "guest:guest", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("testuser1", "testpassword1")]
        [InlineData("testuser2", "testpassword2")]
        [InlineData("guest", "guest")]
        public async Task InitializeAsync_InitializedWithOneSetOfCredentials_ExpectTheCommandToSetupTheCredentials(string username, string password)
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new[]
                    {
                        new SftpUserAccountAttribute { Username = username, Password = password }
                    }
                );
            _containers.Add(container);

            // Act
            await container.InitializeAsync(CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.RunAsync(It.IsAny<string>(), $"{username}:{password}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_InitializedWithMultipleSetOfCredentials_ExpectTheCommandToSetupTheCredentials()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new[]
                    {
                        new SftpUserAccountAttribute { Username = "testuser1", Password = "testpassword1" },
                        new SftpUserAccountAttribute { Username = "testuser2", Password = "testpassword2" },
                        new SftpUserAccountAttribute { Username = "guest", Password = "guest" }
                    }
                );
            _containers.Add(container);

            // Act
            await container.InitializeAsync(CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.RunAsync(It.IsAny<string>(), $"testuser1:testpassword1 testuser2:testpassword2 guest:guest", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenIntitializedWithNoAccounts_ExpectGuestConnectionSettingsToBeSet()
        {
            // Arrange
            var containerIpAddress = "192.168.0.1";

            var expectedHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : containerIpAddress;
            var expectedPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 49621 : 22;

            var expectedRsaMd5Fingerprint = "03:1e:ae:d2:78:33:8e:e2:3d:93:6c:73:95:b5:c3:ca";
            var expectedRsaShaFingerprint = "w8tGM7exiFTGOjsjWccDgj9iSH4mkbvuUHhHK0euOeE";

            var expectedEd25519Md5Fingerprint = "80:b4:c0:dc:dd:e8:4b:5c:2a:01:f5:ec:32:b1:e7:bf";
            var expectedEd25519ShaFingerprint = "tSsvcVKxzqMNPFBrwraCuKCDQy6ADagQz77eOekTfTw";

            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 MD5:{expectedRsaMd5Fingerprint} root@fec96a1bc7dc (RSA)" });
            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 SHA256:{expectedRsaShaFingerprint} root@fec96a1bc7dc (RSA)" });
            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 MD5:{expectedEd25519Md5Fingerprint} root@fec96a1bc7dc (ED25519)" });
            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 SHA256:{expectedEd25519ShaFingerprint} root@fec96a1bc7dc (ED25519)" });

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new SftpUserAccountAttribute[0]);
            _containers.Add(container);

            // Act
            await container.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.Single(container.SftpConnectionSettings);
            Assert.True(container.SftpConnectionSettings.ContainsKey("guest"));

            var connectionSettings = container.SftpConnectionSettings["guest"];

            Assert.Equal("guest", connectionSettings.Username);
            Assert.Equal("guest", connectionSettings.Password);

            Assert.Equal(expectedHost, connectionSettings.Host);

            Assert.Equal(expectedPort, connectionSettings.Port);

            Assert.Equal(expectedRsaShaFingerprint, connectionSettings.RsaFingerprint.Sha256);
            Assert.Equal(expectedRsaMd5Fingerprint, connectionSettings.RsaFingerprint.Md5);

            Assert.Equal(expectedEd25519ShaFingerprint, connectionSettings.Ed25519Fingerprint.Sha256);
            Assert.Equal(expectedEd25519Md5Fingerprint, connectionSettings.Ed25519Fingerprint.Md5);
        }
    }

    public class ConnectionSettingsTests : BaseContainerTests
    {
        [Theory]
        [InlineData("user", "password", "192.168.0.2", 48621, "23:1e:ae:d2:78:33:8e:e2:3d:93:6c:73:95:b5:c3:ca", "28tGM7exiFTGOjsjWccDgj9iSH4mkbvuUHhHK0euOeE", "20:b4:c0:dc:dd:e8:4b:5c:2a:01:f5:ec:32:b1:e7:bf", "2SsvcVKxzqMNPFBrwraCuKCDQy6ADagQz77eOekTfTw")]
        [InlineData("other", "other", "192.168.0.3", 48321, "33:1e:ae:d2:78:33:8e:e2:3d:93:6c:73:95:b5:c3:ca", "38tGM7exiFTGOjsjWccDgj9iSH4mkbvuUHhHK0euOeE", "30:b4:c0:dc:dd:e8:4b:5c:2a:01:f5:ec:32:b1:e7:bf", "3SsvcVKxzqMNPFBrwraCuKCDQy6ADagQz77eOekTfTw")]
        public async Task InitializeAsync_WhenIntitializedWithAnAccount_ExpectAccountConnectionSettingsToBeSet(
            string username,
            string password,
            string containerIpAddress,
            int hostPort,
            string expectedRsaMd5Fingerprint,
            string expectedRsaShaFingerprint,
            string expectedEd25519Md5Fingerprint,
            string expectedEd25519ShaFingerprint
        )
        {
            // Arrange
            var expectedHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : containerIpAddress;
            var expectedPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? hostPort : 22;

            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 MD5:{expectedRsaMd5Fingerprint} root@fec96a1bc7dc (RSA)" });
            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 SHA256:{expectedRsaShaFingerprint} root@fec96a1bc7dc (RSA)" });
            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 MD5:{expectedEd25519Md5Fingerprint} root@fec96a1bc7dc (ED25519)" });
            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 SHA256:{expectedEd25519ShaFingerprint} root@fec96a1bc7dc (ED25519)" });

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new[] { new SftpUserAccountAttribute(username, password) });
            _containers.Add(container);

            // Act
            await container.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.Single(container.SftpConnectionSettings);
            Assert.True(container.SftpConnectionSettings.ContainsKey(username));

            var connectionSettings = container.SftpConnectionSettings[username];

            Assert.Equal(username, connectionSettings.Username);
            Assert.Equal(password, connectionSettings.Password);

            Assert.Equal(expectedHost, connectionSettings.Host);

            Assert.Equal(expectedPort, connectionSettings.Port);

            Assert.Equal(expectedRsaShaFingerprint, connectionSettings.RsaFingerprint.Sha256);
            Assert.Equal(expectedRsaMd5Fingerprint, connectionSettings.RsaFingerprint.Md5);

            Assert.Equal(expectedEd25519ShaFingerprint, connectionSettings.Ed25519Fingerprint.Sha256);
            Assert.Equal(expectedEd25519Md5Fingerprint, connectionSettings.Ed25519Fingerprint.Md5);
        }

        [Theory]
        [InlineData("192.168.0.2", 48621, "23:1e:ae:d2:78:33:8e:e2:3d:93:6c:73:95:b5:c3:ca", "28tGM7exiFTGOjsjWccDgj9iSH4mkbvuUHhHK0euOeE", "20:b4:c0:dc:dd:e8:4b:5c:2a:01:f5:ec:32:b1:e7:bf", "2SsvcVKxzqMNPFBrwraCuKCDQy6ADagQz77eOekTfTw")]
        [InlineData("192.168.0.3", 48321, "33:1e:ae:d2:78:33:8e:e2:3d:93:6c:73:95:b5:c3:ca", "38tGM7exiFTGOjsjWccDgj9iSH4mkbvuUHhHK0euOeE", "30:b4:c0:dc:dd:e8:4b:5c:2a:01:f5:ec:32:b1:e7:bf", "3SsvcVKxzqMNPFBrwraCuKCDQy6ADagQz77eOekTfTw")]
        public async Task InitializeAsync_WhenIntitializedWithMultipleAccounts_ExpectAllAccountConnectionSettingsToBeSet(
            string containerIpAddress,
            int hostPort,
            string expectedRsaMd5Fingerprint,
            string expectedRsaShaFingerprint,
            string expectedEd25519Md5Fingerprint,
            string expectedEd25519ShaFingerprint
        )
        {
            // Arrange
            var accounts = new[]
            {
                new SftpUserAccountAttribute("user", "password"),
                new SftpUserAccountAttribute("test", "test")
            };

            var expectedHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : containerIpAddress;
            var expectedPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? hostPort : 22;

            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 MD5:{expectedRsaMd5Fingerprint} root@fec96a1bc7dc (RSA)" });
            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 SHA256:{expectedRsaShaFingerprint} root@fec96a1bc7dc (RSA)" });
            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 MD5:{expectedEd25519Md5Fingerprint} root@fec96a1bc7dc (ED25519)" });
            containerGatewayMock.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 SHA256:{expectedEd25519ShaFingerprint} root@fec96a1bc7dc (ED25519)" });

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, accounts);
            _containers.Add(container);

            // Act
            await container.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.Equal(accounts.Length, container.SftpConnectionSettings.Count);

            foreach (var account in accounts)
            {
                Assert.True(container.SftpConnectionSettings.ContainsKey(account.Username));

                var connectionSettings = container.SftpConnectionSettings[account.Username];

                Assert.Equal(account.Username, connectionSettings.Username);
                Assert.Equal(account.Password, connectionSettings.Password);

                Assert.Equal(expectedHost, connectionSettings.Host);

                Assert.Equal(expectedPort, connectionSettings.Port);

                Assert.Equal(expectedRsaShaFingerprint, connectionSettings.RsaFingerprint.Sha256);
                Assert.Equal(expectedRsaMd5Fingerprint, connectionSettings.RsaFingerprint.Md5);

                Assert.Equal(expectedEd25519ShaFingerprint, connectionSettings.Ed25519Fingerprint.Sha256);
                Assert.Equal(expectedEd25519Md5Fingerprint, connectionSettings.Ed25519Fingerprint.Md5);
            }
        }
    }

    public class FileTests : BaseContainerTests
    {
        [Theory]
        [InlineData("file/one", "admin", "destination/one", "testowner", "testpermissions", "/home/admin/destination/one")]
        [InlineData("two", "tswift", "/two.txt", "owner", "permissions", "/home/tswift/two.txt")]
        public async Task AddUserFileAsync_WhenCalled_ExpectToBeForwardedToTheGatewayWithTheFullPath(string hostFilename, string user, string containerFilename, string owner, string permissions, string expectedContainerFilename)
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new[] { new SftpUserAccountAttribute(user, "password") });
            _containers.Add(container);

            var cancellationToken = new CancellationToken();

            // Act
            await container.AddUserFileAsync(user, hostFilename, containerFilename, owner, permissions, cancellationToken);

            // Assert
            containerGatewayMock.Verify(x => x.AddFileAsync(container.Id, hostFilename, expectedContainerFilename, owner, permissions, cancellationToken), Times.Once);
        }

        [Theory]
        [InlineData("admin", "destination/one", "/home/admin/destination/one")]
        [InlineData("tswift", "/two.txt", "/home/tswift/two.txt")]
        public async Task RemoveUserFileAsync_WhenCalled_ExpectToBeForwardedToTheGatewayWithTheFullPath(string user, string containerFilename, string expectedContainerFilename)
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new[] { new SftpUserAccountAttribute(user, "password") });
            _containers.Add(container);

            var cancellationToken = new CancellationToken();

            // Act
            await container.RemoveUserFileAsync(user, containerFilename, CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.RemoveFileAsync(container.Id, expectedContainerFilename, cancellationToken), Times.Once);
        }

        [Theory]
        [InlineData("admin", "destination/one", "testowner", "testpermissions", "/home/admin/destination/one")]
        [InlineData("tswift", "/two.txt", "owner", "permissions", "/home/tswift/two.txt")]
        public async Task CreateUserFileAsync_WhenCalledWithAString_ExpectToBeForwardedToTheGatewayWithTheFullPath(string user, string containerFilename, string owner, string permissions, string expectedContainerFilename)
        {
            // Arrange
            var fileContents = Guid.NewGuid().ToString();

            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new[] { new SftpUserAccountAttribute(user, "password") });
            _containers.Add(container);

            var actualFilename = default(string);
            var actualContents = default(string);

            containerGatewayMock.Setup(x => x.AddFileAsync(container.Id, It.IsAny<string>(), expectedContainerFilename, owner, permissions, It.IsAny<CancellationToken>()))
                .Callback<string, string, string, string, string, CancellationToken>((_, hostFilename, _, _, _, _) =>
                {
                    actualFilename = hostFilename;
                    actualContents = File.ReadAllText(hostFilename);
                });

            var cancellationToken = new CancellationToken();

            // Act
            await container.CreateUserFileAsync(user, fileContents, containerFilename, owner, permissions, CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.AddFileAsync(container.Id, It.Is<string>(x => x.StartsWith(Path.GetTempPath())), expectedContainerFilename, owner, permissions, cancellationToken), Times.Once);
            Assert.False(File.Exists(actualFilename));
            Assert.Equal(fileContents, actualContents);
        }

        [Theory]
        [InlineData("admin", "destination/one", "testowner", "testpermissions", "/home/admin/destination/one")]
        [InlineData("tswift", "/two.txt", "owner", "permissions", "/home/tswift/two.txt")]
        public async Task CreateUserFileAsync_WhenCalledWithAStream_ExpectToBeForwardedToTheGatewayWithTheFullPath(string user, string containerFilename, string owner, string permissions, string expectedContainerFilename)
        {
            // Arrange
            var fileContents = Guid.NewGuid().ToString();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContents));

            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new[] { new SftpUserAccountAttribute(user, "password") });
            _containers.Add(container);

            var actualFilename = default(string);
            var actualContents = default(string);

            containerGatewayMock.Setup(x => x.AddFileAsync(container.Id, It.IsAny<string>(), expectedContainerFilename, owner, permissions, It.IsAny<CancellationToken>()))
                .Callback<string, string, string, string, string, CancellationToken>((_, hostFilename, _, _, _, _) =>
                {
                    actualFilename = hostFilename;
                    actualContents = File.ReadAllText(hostFilename);
                });

            var cancellationToken = new CancellationToken();

            // Act
            await container.CreateUserFileAsync(user, stream, containerFilename, owner, permissions, CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.AddFileAsync(container.Id, It.Is<string>(x => x.StartsWith(Path.GetTempPath())), expectedContainerFilename, owner, permissions, cancellationToken), Times.Once);
            Assert.False(File.Exists(actualFilename));
            Assert.Equal(fileContents, actualContents);
        }

        [Theory]
        [InlineData("admin")]
        [InlineData("tswift")]
        public async Task EmptyUserDirectoryAsync_WhenCalled_ExpectUsersDirectoryToBeEmptied(string user)
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new SftpContainer(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new[] { new SftpUserAccountAttribute(user, "password") });
            _containers.Add(container);

            // Act
            await container.EmptyUserDirectoryAsync(user, CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.EmptyDirectoryAsync(container.Id, $"/home/{user}", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

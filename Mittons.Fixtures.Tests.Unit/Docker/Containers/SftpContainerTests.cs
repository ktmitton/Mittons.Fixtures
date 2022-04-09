using Xunit;
using Moq;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Docker.Containers;
using System.Linq;
using System;
using Mittons.Fixtures.Docker.Attributes;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers
{
    public class SftpContainerTests : IDisposable
    {
        private readonly string sftpImageName = "atmoz/sftp:alpine";

        private readonly List<Container> _containers = new List<Container>();

        public void Dispose()
        {
            foreach(var container in _containers)
            {
                container.DisposeAsync().GetAwaiter().GetResult();
            }
        }

        [Fact]
        public async Task InitializeAsync_WhenCalled_ExpectTheContainerToUseTheAtmozImage()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);

            var container = new SftpContainer(gatewayMock.Object, Enumerable.Empty<Attribute>());
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

            // Assert
            gatewayMock.Verify(x => x.ContainerRunAsync(sftpImageName, It.IsAny<string>(), It.Is<Dictionary<string, string>>(x => x.Count == 1 && x.ContainsKey("mittons.fixtures.run.id")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_InitializedWithNoCredentials_ExpectTheCommandToSetupTheGuestAccount()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);

            var container = new SftpContainer(gatewayMock.Object, Enumerable.Empty<Attribute>());
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

            // Assert
            gatewayMock.Verify(x => x.ContainerRunAsync(sftpImageName, "guest:guest", It.Is<Dictionary<string, string>>(x => x.Count == 1 && x.ContainsKey("mittons.fixtures.run.id")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("testuser1", "testpassword1")]
        [InlineData("testuser2", "testpassword2")]
        [InlineData("guest", "guest")]
        public async Task InitializeAsync_InitializedWithOneSetOfCredentials_ExpectTheCommandToSetupTheCredentials(string username, string password)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);

            var container = new SftpContainer(
                    gatewayMock.Object,
                    new SftpUserAccount[]
                    {
                        new SftpUserAccount { Username = username, Password = password }
                    }
                );
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

            // Assert
            gatewayMock.Verify(x => x.ContainerRunAsync(sftpImageName, $"{username}:{password}", It.Is<Dictionary<string, string>>(x => x.Count == 1 && x.ContainsKey("mittons.fixtures.run.id")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_InitializedWithMultipleSetOfCredentials_ExpectTheCommandToSetupTheCredentials()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);

            var container = new SftpContainer(
                    gatewayMock.Object,
                    new SftpUserAccount[]
                    {
                        new SftpUserAccount { Username = "testuser1", Password = "testpassword1" },
                        new SftpUserAccount { Username = "testuser2", Password = "testpassword2" },
                        new SftpUserAccount { Username = "guest", Password = "guest" }
                    }
                );
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

            // Assert
            gatewayMock.Verify(x => x.ContainerRunAsync(sftpImageName, $"testuser1:testpassword1 testuser2:testpassword2 guest:guest", It.Is<Dictionary<string, string>>(x => x.Count == 1 && x.ContainsKey("mittons.fixtures.run.id")), It.IsAny<CancellationToken>()), Times.Once);
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

            var gatewayMock = new Mock<IDockerGateway>();

            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Parse(containerIpAddress));

            gatewayMock.Setup(x => x.ContainerGetHostPortMappingAsync(It.IsAny<string>(), "tcp", 22, CancellationToken.None)).ReturnsAsync(49621);

            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 MD5:{expectedRsaMd5Fingerprint} root@fec96a1bc7dc (RSA)" });
            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 SHA256:{expectedRsaShaFingerprint} root@fec96a1bc7dc (RSA)" });
            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 MD5:{expectedEd25519Md5Fingerprint} root@fec96a1bc7dc (ED25519)" });
            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 SHA256:{expectedEd25519ShaFingerprint} root@fec96a1bc7dc (ED25519)" });

            var container = new SftpContainer(gatewayMock.Object, new SftpUserAccount[0]);
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

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

            var gatewayMock = new Mock<IDockerGateway>();

            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Parse(containerIpAddress));

            gatewayMock.Setup(x => x.ContainerGetHostPortMappingAsync(It.IsAny<string>(), "tcp", 22, CancellationToken.None)).ReturnsAsync(hostPort);

            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 MD5:{expectedRsaMd5Fingerprint} root@fec96a1bc7dc (RSA)" });
            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 SHA256:{expectedRsaShaFingerprint} root@fec96a1bc7dc (RSA)" });
            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 MD5:{expectedEd25519Md5Fingerprint} root@fec96a1bc7dc (ED25519)" });
            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 SHA256:{expectedEd25519ShaFingerprint} root@fec96a1bc7dc (ED25519)" });

            var container = new SftpContainer(gatewayMock.Object, new SftpUserAccount[] { new SftpUserAccount(username, password) });
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

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
                new SftpUserAccount("user", "password"),
                new SftpUserAccount("test", "test")
            };

            var expectedHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : containerIpAddress;
            var expectedPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? hostPort : 22;

            var gatewayMock = new Mock<IDockerGateway>();

            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Parse(containerIpAddress));

            gatewayMock.Setup(x => x.ContainerGetHostPortMappingAsync(It.IsAny<string>(), "tcp", 22, CancellationToken.None)).ReturnsAsync(hostPort);

            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 MD5:{expectedRsaMd5Fingerprint} root@fec96a1bc7dc (RSA)" });
            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_rsa_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"4096 SHA256:{expectedRsaShaFingerprint} root@fec96a1bc7dc (RSA)" });
            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 MD5:{expectedEd25519Md5Fingerprint} root@fec96a1bc7dc (ED25519)" });
            gatewayMock.Setup(x => x.ContainerExecuteCommandAsync(It.IsAny<string>(), "ssh-keygen -l -E sha256 -f /etc/ssh/ssh_host_ed25519_key.pub", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { $"256 SHA256:{expectedEd25519ShaFingerprint} root@fec96a1bc7dc (ED25519)" });

            var container = new SftpContainer(gatewayMock.Object, accounts);
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

            // Assert
            Assert.Equal(accounts.Length, container.SftpConnectionSettings.Count);

            foreach(var account in accounts)
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
}
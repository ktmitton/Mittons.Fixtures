using System;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Fixtures;
using Mittons.Fixtures.Docker.Gateways;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Environments
{
    public class DockerEnvironmentTests
    {
        public class ContainerTests
        {
            private class ContainerTestEnvironmentFixture : DockerEnvironmentFixture
            {
                [Image("alpine:3.15")]
                public Container? AlpineContainer { get; set; }

                [Image("node:17-alpine3.15")]
                public Container? NodeContainer { get; set; }

                public ContainerTestEnvironmentFixture(IDockerGateway dockerGateway)
                    : base(dockerGateway)
                {
                }
            }

            [Fact]
            public void Ctor_WhenInitializedWithContainerDefinitions_ExpectContainersToRunUsingTheDefinedImages()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();

                // Act
                using var fixture = new ContainerTestEnvironmentFixture(gatewayMock.Object);

                // Assert
                gatewayMock.Verify(x => x.ContainerRun("alpine:3.15", string.Empty), Times.Once);
                gatewayMock.Verify(x => x.ContainerRun("node:17-alpine3.15", string.Empty), Times.Once);
            }

            [Fact]
            public void Dispose_WhenCalled_ExpectAllContainersToBeRemoved()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.ContainerRun("alpine:3.15", string.Empty)).Returns("runningid");
                gatewayMock.Setup(x => x.ContainerRun("node:17-alpine3.15", string.Empty)).Returns("disposingid");

                using var fixture = new ContainerTestEnvironmentFixture(gatewayMock.Object);

                // Act
                fixture.Dispose();

                // Assert
                Assert.NotNull(fixture.AlpineContainer);
                Assert.NotNull(fixture.NodeContainer);

                if (fixture.AlpineContainer is null || fixture.NodeContainer is null)
                {
                    return;
                }

                gatewayMock.Verify(x => x.ContainerRemove(fixture.AlpineContainer.Id), Times.Once);
                gatewayMock.Verify(x => x.ContainerRemove(fixture.NodeContainer.Id), Times.Once);
            }
        }

        public class SftpContainerTests
        {
            private class SftpContainerTestEnvironmentFixture : DockerEnvironmentFixture
            {
                public SftpContainer? GuestContainer { get; set; }

                [SftpUserAccount("testuser1", "testpassword1")]
                [SftpUserAccount(Username = "testuser2", Password = "testpassword2")]
                public SftpContainer? AccountsContainer { get; set; }

                public SftpContainerTestEnvironmentFixture(IDockerGateway dockerGateway)
                    : base(dockerGateway)
                {
                }
            }

            [Fact]
            public void Ctor_WhenInitializedWithSftpContainerDefinitions_ExpectContainersToRunUsingTheSftpImage()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();

                // Act
                using var fixture = new SftpContainerTestEnvironmentFixture(gatewayMock.Object);

                // Assert
                gatewayMock.Verify(x => x.ContainerRun("atmoz/sftp", It.IsAny<string>()), Times.Exactly(2));
            }

            [Fact]
            public void Dispose_WhenCalled_ExpectAllContainersToBeRemoved()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.ContainerRun("atmoz/sftp", "guest:guest")).Returns("guest");
                gatewayMock.Setup(x => x.ContainerRun("atmoz/sftp", "testuser1:testpassword1 testuser2:testpassword2")).Returns("account");

                using var fixture = new SftpContainerTestEnvironmentFixture(gatewayMock.Object);

                // Act
                fixture.Dispose();

                // Assert
                Assert.NotNull(fixture.GuestContainer);
                Assert.NotNull(fixture.AccountsContainer);

                if (fixture.GuestContainer is null || fixture.AccountsContainer is null)
                {
                    return;
                }

                gatewayMock.Verify(x => x.ContainerRemove(fixture.GuestContainer.Id), Times.Once);
                gatewayMock.Verify(x => x.ContainerRemove(fixture.AccountsContainer.Id), Times.Once);
            }
        }

        public class NetworkTests
        {
            [Network("network1")]
            [Network("network2")]
            private class NetworkTestEnvironmentFixture : DockerEnvironmentFixture
            {
                [Image("alpine:3.15")]
                [NetworkAlias("network1", "alpine.example.com")]
                public Container? AlpineContainer { get; set; }

                [NetworkAlias("network1", "sftp.example.com")]
                [NetworkAlias("network2", "sftp-other.example.com")]
                public SftpContainer? SftpContainer { get; set; }

                public NetworkTestEnvironmentFixture(IDockerGateway dockerGateway)
                    : base(dockerGateway)
                {
                }
            }

            [Network("network1")]
            [Network("network1")]
            private class DuplicateNetworkTestEnvironmentFixture : DockerEnvironmentFixture
            {
                [Image("alpine:3.15")]
                public Container? GuestContainer { get; set; }

                public SftpContainer? AccountsContainer { get; set; }

                public DuplicateNetworkTestEnvironmentFixture(IDockerGateway dockerGateway)
                    : base(dockerGateway)
                {
                }
            }

            [Fact]
            public void Ctor_WhenNetworksAreDefinedForAFixture_ExpectTheNetworksToBeCreated()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();

                // Act
                using var fixture = new NetworkTestEnvironmentFixture(gatewayMock.Object);

                // Assert
                gatewayMock.Verify(x => x.NetworkCreate($"network1-{fixture.InstanceId}"), Times.Once);
                gatewayMock.Verify(x => x.NetworkCreate($"network2-{fixture.InstanceId}"), Times.Once);
            }

            [Fact]
            public void Ctor_WhenDuplicateNetworksAreDefinedForAFixture_ExpectAnErrorToBeThrown()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();

                // Act
                // Assert
                Assert.Throws<NotSupportedException>(() => new DuplicateNetworkTestEnvironmentFixture(gatewayMock.Object));
            }

            [Fact]
            public void Ctor_WhenDuplicateNetworksAreCreatedForDifferentFixtures_ExpectTheNetworksToBeCreatedAndScopedToTheirFixture()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();

                // Act
                using var fixture1 = new NetworkTestEnvironmentFixture(gatewayMock.Object);
                using var fixture2 = new NetworkTestEnvironmentFixture(gatewayMock.Object);

                // Assert
                gatewayMock.Verify(x => x.NetworkCreate($"network1-{fixture1.InstanceId}"), Times.Once);
                gatewayMock.Verify(x => x.NetworkCreate($"network2-{fixture1.InstanceId}"), Times.Once);
                gatewayMock.Verify(x => x.NetworkCreate($"network1-{fixture2.InstanceId}"), Times.Once);
                gatewayMock.Verify(x => x.NetworkCreate($"network2-{fixture2.InstanceId}"), Times.Once);
            }

            [Fact]
            public void Ctor_WhenContainersHaveDefinedNetworkAliases_ExpectTheContainersToBeConnectedToTheDefinedNetworks()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();

                // Act
                using var fixture = new NetworkTestEnvironmentFixture(gatewayMock.Object);

                // Assert
                Assert.NotNull(fixture.AlpineContainer);
                Assert.NotNull(fixture.SftpContainer);

                if (fixture.AlpineContainer is null || fixture.SftpContainer is null)
                {
                    return;
                }

                gatewayMock.Verify(x => x.NetworkConnect($"network1-{fixture.InstanceId}", fixture.AlpineContainer.Id, "alpine.example.com"), Times.Once);
                gatewayMock.Verify(x => x.NetworkConnect($"network1-{fixture.InstanceId}", fixture.SftpContainer.Id, "sftp.example.com"), Times.Once);
                gatewayMock.Verify(x => x.NetworkConnect($"network2-{fixture.InstanceId}", fixture.SftpContainer.Id, "sftp-other.example.com"), Times.Once);
            }

            [Fact]
            public void Dispose_WhenCalled_ExpectNetworksToBeRemoved()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();

                // Act
                using var fixture = new NetworkTestEnvironmentFixture(gatewayMock.Object);
                fixture.Dispose();

                // Assert
                gatewayMock.Verify(x => x.NetworkRemove($"network1-{fixture.InstanceId}"), Times.Once);
                gatewayMock.Verify(x => x.NetworkRemove($"network2-{fixture.InstanceId}"), Times.Once);
            }
        }
    }
}
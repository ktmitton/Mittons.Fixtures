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
                public Container AlpineContainer { get; set; }

                [Image("node:17-alpine3.15")]
                public Container NodeContainer { get; set; }

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
                gatewayMock.Verify(x => x.Run("alpine:3.15", string.Empty), Times.Once);
                gatewayMock.Verify(x => x.Run("node:17-alpine3.15", string.Empty), Times.Once);
            }

            [Fact]
            public void Dispose_WhenCalled_ExpectAllContainersToBeRemoved()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.Run("alpine:3.15", string.Empty)).Returns("runningid");
                gatewayMock.Setup(x => x.Run("node:17-alpine3.15", string.Empty)).Returns("disposingid");

                using var fixture = new ContainerTestEnvironmentFixture(gatewayMock.Object);

                // Act
                fixture.Dispose();

                // Assert
                gatewayMock.Verify(x => x.Remove(fixture.AlpineContainer.Id), Times.Once);
                gatewayMock.Verify(x => x.Remove(fixture.NodeContainer.Id), Times.Once);
            }
        }

        public class SftpContainerTests
        {
            private class SftpContainerTestEnvironmentFixture : DockerEnvironmentFixture
            {
                public SftpContainer GuestContainer { get; set; }

                [SftpUserAccount("testuser1", "testpassword1")]
                [SftpUserAccount(Username = "testuser2", Password = "testpassword2")]
                public SftpContainer AccountsContainer { get; set; }

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
                gatewayMock.Verify(x => x.Run("atmoz/sftp", It.IsAny<string>()), Times.Exactly(2));
            }

            [Fact]
            public void Dispose_WhenCalled_ExpectAllContainersToBeRemoved()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.Run("atmoz/sftp", "guest:guest")).Returns("guest");
                gatewayMock.Setup(x => x.Run("atmoz/sftp", "testuser1:testpassword1 testuser2:testpassword2")).Returns("account");

                using var fixture = new SftpContainerTestEnvironmentFixture(gatewayMock.Object);

                // Act
                fixture.Dispose();

                // Assert
                gatewayMock.Verify(x => x.Remove(fixture.GuestContainer.Id), Times.Once);
                gatewayMock.Verify(x => x.Remove(fixture.AccountsContainer.Id), Times.Once);
            }
        }
    }
}
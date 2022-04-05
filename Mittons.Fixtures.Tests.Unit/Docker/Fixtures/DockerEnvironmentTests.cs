using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Fixtures;
using Mittons.Fixtures.Docker.Gateways;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Environments
{
    public class DockerEnvironmentTests
    {
        public class Containers
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
                var fixture = new ContainerTestEnvironmentFixture(gatewayMock.Object);

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

                var fixture = new ContainerTestEnvironmentFixture(gatewayMock.Object);

                // Act
                fixture.Dispose();

                // Assert
                gatewayMock.Verify(x => x.Remove(fixture.AlpineContainer.Id), Times.Once);
                gatewayMock.Verify(x => x.Remove(fixture.NodeContainer.Id), Times.Once);
            }
        }
    }
}
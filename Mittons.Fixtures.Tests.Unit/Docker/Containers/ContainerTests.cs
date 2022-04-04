using Xunit;
using Moq;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Docker.Containers;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers
{
    public class ContainerTests
    {
        [Theory]
        [InlineData("myimage")]
        [InlineData("otherimage")]
        public void Ctor_WhenInitializedWithAnImageName_ExpectADockerRunCommandToIncludeTheImage(string image)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            // Act
            using var container = new Container(gatewayMock.Object, image);

            // Assert
            gatewayMock.Verify(x => x.Run(image), Times.Once);
        }

        [Fact]
        public void Dispose_WhenCalled_ExpectADockerRemoveCommandToBeExecuted()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            using var container = new Container(gatewayMock.Object, string.Empty);

            // Act
            container.Dispose();

            // Assert
            gatewayMock.Verify(x => x.Remove(container.Id), Times.Once);
        }

        [Fact]
        public void Dispose_WhenCalledWhileAnotherContainerIsRunning_ExpectOnlyTheCalledContainerToBeRemoved()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.Run("runningimage")).Returns("runningid");
            gatewayMock.Setup(x => x.Run("disposingimage")).Returns("disposingid");

            using var runningContainer = new Container(gatewayMock.Object, "runningimage");
            using var disposingContainer = new Container(gatewayMock.Object, "disposingimage");

            // Act
            disposingContainer.Dispose();

            // Assert
            gatewayMock.Verify(x => x.Remove(disposingContainer.Id), Times.Once);
            gatewayMock.Verify(x => x.Remove(runningContainer.Id), Times.Never);
        }
    }
}
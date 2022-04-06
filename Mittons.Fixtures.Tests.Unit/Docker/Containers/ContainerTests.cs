using Xunit;
using Moq;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Docker.Containers;
using System;
using Mittons.Fixtures.Docker.Attributes;
using System.Net;
using System.IO;
using System.Text;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers
{
    public class ContainerTests
    {
        [Theory]
        [InlineData("myimage")]
        [InlineData("otherimage")]
        public void Ctor_WhenInitializedWithAnImageName_ExpectTheImageNameToBePassedToTheDockerRunCommand(string imageName)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            // Act
            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(imageName), new Command(string.Empty) });

            // Assert
            gatewayMock.Verify(x => x.ContainerRun(imageName, string.Empty), Times.Once);
        }

        [Theory]
        [InlineData("mycommand")]
        [InlineData("othercommand")]
        public void Ctor_WhenInitializedWithACommand_ExpectTheCommandToBePassedToTheDockerRunCommand(string command)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            // Act
            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(command) });

            // Assert
            gatewayMock.Verify(x => x.ContainerRun(string.Empty, command), Times.Once);
        }

        [Fact]
        public void Dispose_WhenCalled_ExpectADockerRemoveCommandToBeExecuted()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            // Act
            container.Dispose();

            // Assert
            gatewayMock.Verify(x => x.ContainerRemove(container.Id), Times.Once);
        }

        [Fact]
        public void Dispose_WhenCalledWhileAnotherContainerIsRunning_ExpectOnlyTheCalledContainerToBeRemoved()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerRun("runningimage", string.Empty)).Returns("runningid");
            gatewayMock.Setup(x => x.ContainerRun("disposingimage", string.Empty)).Returns("disposingid");

            using var runningContainer = new Container(gatewayMock.Object, new Attribute[] { new Image("runningimage"), new Command(string.Empty) });
            using var disposingContainer = new Container(gatewayMock.Object, new Attribute[] { new Image("disposingimage"), new Command(string.Empty) });

            // Act
            disposingContainer.Dispose();

            // Assert
            gatewayMock.Verify(x => x.ContainerRemove(disposingContainer.Id), Times.Once);
            gatewayMock.Verify(x => x.ContainerRemove(runningContainer.Id), Times.Never);
        }

        [Theory]
        [InlineData("192.168.0.0")]
        [InlineData("192.168.0.1")]
        [InlineData("127.0.0.1")]
        public void Ctor_WhenCreated_ExpectTheDefaultIpAddressToBeSet(string ipAddress)
        {
            // Arrange
            var parsed = IPAddress.Parse(ipAddress);

            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddress(It.IsAny<string>())).Returns(parsed);

            // Act
            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            // Assert
            Assert.Equal(parsed, container.IpAddress);
        }

        [Theory]
        [InlineData("file/one", "destination/one", "testowner", "testpermissions")]
        [InlineData("two", "two", "owner", "permissions")]
        public void AddFile_WhenCalled_ExpectDetailsToBeForwardedToTheGateway(string hostFilename, string containerFilename, string owner, string permissions)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            // Act
            container.AddFile(hostFilename, containerFilename, owner, permissions);

            // Assert
            gatewayMock.Verify(x => x.ContainerAddFile(container.Id, hostFilename, containerFilename, owner, permissions), Times.Once);
        }

        [Theory]
        [InlineData("destination/one")]
        [InlineData("two")]
        public void RemoveFile_WhenCalled_ExpectDetailsToBeForwardedToTheGateway(string containerFilename)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            // Act
            container.RemoveFile(containerFilename);

            // Assert
            gatewayMock.Verify(x => x.ContainerRemoveFile(container.Id, containerFilename), Times.Once);
        }

        [Theory]
        [InlineData("destination/one", "testowner", "testpermissions")]
        [InlineData("two", "owner", "permissions")]
        public void CreateFile_WhenCalledWithAString_ExpectGatewayToBeCalledWithATemporaryFile(string containerFilename, string owner, string permissions)
        {
            // Arrange
            var fileContents = Guid.NewGuid().ToString();

            var gatewayMock = new Mock<IDockerGateway>();

            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            var actualFilename = default(string);
            var actualContents = default(string);

            gatewayMock.Setup(x => x.ContainerAddFile(container.Id, It.IsAny<string>(), containerFilename, owner, permissions))
                .Callback<string, string, string, string, string>((_, hostFilename, _, _, _) =>
                {
                    actualFilename = hostFilename;
                    actualContents = File.ReadAllText(hostFilename);
                });

            // Act
            container.CreateFile(fileContents, containerFilename, owner, permissions);

            // Assert
            Assert.Equal(Path.GetDirectoryName(Path.GetTempPath()), Path.GetDirectoryName(actualFilename));
            Assert.False(File.Exists(actualFilename));
            Assert.Equal(fileContents, actualContents);
        }

        [Theory]
        [InlineData("destination/one", "testowner", "testpermissions")]
        [InlineData("two", "owner", "permissions")]
        public void CreateFile_WhenCalledWithAStream_ExpectGatewayToBeCalledWithATemporaryFile(string containerFilename, string owner, string permissions)
        {
            // Arrange
            var fileContents = Guid.NewGuid().ToString();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContents));

            var gatewayMock = new Mock<IDockerGateway>();

            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            var actualFilename = default(string);
            var actualContents = default(string);

            gatewayMock.Setup(x => x.ContainerAddFile(container.Id, It.IsAny<string>(), containerFilename, owner, permissions))
                .Callback<string, string, string, string, string>((_, hostFilename, _, _, _) =>
                {
                    actualFilename = hostFilename;
                    actualContents = File.ReadAllText(hostFilename);
                });

            // Act
            container.CreateFile(stream, containerFilename, owner, permissions);

            // Assert
            Assert.Equal(Path.GetDirectoryName(Path.GetTempPath()), Path.GetDirectoryName(actualFilename));
            Assert.False(File.Exists(actualFilename));
            Assert.Equal(fileContents, actualContents);
        }
    }
}
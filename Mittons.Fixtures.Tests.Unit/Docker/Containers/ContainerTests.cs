using Xunit;
using Moq;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Docker.Containers;
using System;
using Mittons.Fixtures.Docker.Attributes;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

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
            gatewayMock.Verify(x => x.ContainerRun(imageName, string.Empty, It.Is<Dictionary<string, string>>(x => x.Count == 1 && x.ContainsKey("mittons.fixtures.run.id"))), Times.Once);
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
            gatewayMock.Verify(x => x.ContainerRun(string.Empty, command, It.Is<Dictionary<string, string>>(x => x.Count == 1 && x.ContainsKey("mittons.fixtures.run.id"))), Times.Once);
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

        [Fact]
        public void Ctor_WhenCreatedWithARun_ExpectLabelsToBePassedToTheGateway()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            var run = new Run();

            // Act
            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty), run });

            // Assert
            gatewayMock.Verify(x => x.ContainerRun(string.Empty, string.Empty, It.Is<Dictionary<string, string>>(x => x.ContainsKey("mittons.fixtures.run.id") && x["mittons.fixtures.run.id"] == run.Id)));
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
            gatewayMock.Setup(x => x.ContainerRun("runningimage", string.Empty, It.Is<Dictionary<string, string>>(x => x.Count == 1 && x.ContainsKey("mittons.fixtures.run.id"))))
                .Returns("runningid");
            gatewayMock.Setup(x => x.ContainerRun("disposingimage", string.Empty, It.Is<Dictionary<string, string>>(x => x.Count == 1 && x.ContainsKey("mittons.fixtures.run.id"))))
                .Returns("disposingid");

            using var runningContainer = new Container(gatewayMock.Object, new Attribute[] { new Image("runningimage"), new Command(string.Empty) });
            using var disposingContainer = new Container(gatewayMock.Object, new Attribute[] { new Image("disposingimage"), new Command(string.Empty) });

            // Act
            disposingContainer.Dispose();

            // Assert
            gatewayMock.Verify(x => x.ContainerRemove(disposingContainer.Id), Times.Once);
            gatewayMock.Verify(x => x.ContainerRemove(runningContainer.Id), Times.Never);
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

        [Theory]
        [InlineData(HealthStatus.Healthy)]
        [InlineData(HealthStatus.Running)]
        public async Task StartAsync_WhenContainerHealthIsPassing_ExpectContainerIdToBeReturned(HealthStatus status)
        {
            // Arrange
            var cancellationToken = new CancellationToken();

            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(status);

            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            // Act
            var containerId = await container.StartAsync(TimeSpan.FromMilliseconds(10), cancellationToken);

            // Assert
            Assert.Equal(container.Id, containerId);
        }

        [Fact]
        public async Task StartAsync_WhenContainerBecomesHealthy_ExpectContainerIdToBeReturned()
        {
            // Arrange
            var cancellationToken = new CancellationToken();

            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.SetupSequence(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Unknown)
                .ReturnsAsync(HealthStatus.Unknown)
                .ReturnsAsync(HealthStatus.Healthy);

            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();

            var containerId = await container.StartAsync(TimeSpan.FromMilliseconds(250), cancellationToken);

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.Elapsed > TimeSpan.FromMilliseconds(50));
            Assert.Equal(container.Id, containerId);
        }

        [Theory]
        [InlineData(HealthStatus.Unknown)]
        [InlineData(HealthStatus.Unhealthy)]
        public async Task StartAsync_WhenContainerHealthNeverPassesBeforeProvidedTimeout_ExpectExceptionToBeThrown(HealthStatus status)
        {
            // Arrange
            var cancellationToken = new CancellationToken();

            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(status);

            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            // Act
            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => container.StartAsync(TimeSpan.FromMilliseconds(10), cancellationToken));
        }

        [Fact]
        public async Task StartAsync_WhenContainerHealthNeverPassesBeforeRequestCancellation_ExpectExceptionToBeThrown()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Unknown);

            using var container = new Container(gatewayMock.Object, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });

            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();

            var startTask = container.StartAsync(TimeSpan.FromSeconds(10), cancellationToken);

            Assert.False(startTask.IsCompleted);

            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => startTask);

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10));
        }
    }
}
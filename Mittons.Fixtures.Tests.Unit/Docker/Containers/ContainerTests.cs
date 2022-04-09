using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Gateways;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers
{
    public class BaseContainerTests : IAsyncDisposable
    {
        protected readonly List<Container> _containers = new List<Container>();

        public async ValueTask DisposeAsync()
        {
            foreach (var container in _containers)
            {
                await container.DisposeAsync();
            }
        }
    }

    public class ContainerTests
    {
        public class InitializeTests
        {
            public class ImageTests : BaseContainerTests
            {
                [Theory]
                [InlineData("myimage")]
                [InlineData("otherimage")]
                public async Task InitializeAsync_WhenInitializedWithAnImageName_ExpectTheImageNameToBePassedToTheDockerRunCommand(string imageName)
                {
                    // Arrange
                    var gatewayMock = new Mock<IDockerGateway>();
                    gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(HealthStatus.Healthy);

                    var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(imageName), new Command(string.Empty) });
                    _containers.Add(container);

                    // Act
                    await container.InitializeAsync();

                    // Assert
                    gatewayMock.Verify(x => x.ContainerRunAsync(imageName, string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }

            public class CommandTests : BaseContainerTests
            {
                [Theory]
                [InlineData("mycommand")]
                [InlineData("othercommand")]
                public async Task InitializeAsync_WhenInitializedWithACommand_ExpectTheCommandToBePassedToTheDockerRunCommand(string command)
                {
                    // Arrange
                    var gatewayMock = new Mock<IDockerGateway>();
                    gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(HealthStatus.Healthy);

                    var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(command) });
                    _containers.Add(container);

                    // Act
                    await container.InitializeAsync();

                    // Assert
                    gatewayMock.Verify(x => x.ContainerRunAsync(string.Empty, command, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }

            public class NetworkTests : BaseContainerTests
            {
                [Theory]
                [InlineData("192.168.0.0")]
                [InlineData("192.168.0.1")]
                [InlineData("127.0.0.1")]
                public async Task InitializeAsync_WhenCreated_ExpectTheDefaultIpAddressToBeSet(string ipAddress)
                {
                    // Arrange
                    var parsed = IPAddress.Parse(ipAddress);

                    var gatewayMock = new Mock<IDockerGateway>();
                    gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(parsed);
                    gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(HealthStatus.Healthy);

                    var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });
                    _containers.Add(container);

                    // Act
                    await container.InitializeAsync();

                    // Assert
                    Assert.Equal(parsed, container.IpAddress);
                }
            }

            public class LabelTests : BaseContainerTests
            {
                [Fact]
                public async Task InitializeAsync_WhenCreatedWithARun_ExpectLabelsToBePassedToTheGateway()
                {
                    // Arrange
                    var gatewayMock = new Mock<IDockerGateway>();
                    gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(HealthStatus.Healthy);
                    gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(HealthStatus.Healthy);

                    var run = new Run();

                    var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty), run });
                    _containers.Add(container);

                    // Act
                    await container.InitializeAsync();

                    // Assert
                    gatewayMock.Verify(
                            x =>
                            x.ContainerRunAsync(
                                string.Empty,
                                string.Empty,
                                It.Is<IEnumerable<KeyValuePair<string, string>>>(x => x.Any(y => y.Key == "--label" && y.Value == $"mittons.fixtures.run.id={run.Id}")),
                                It.IsAny<CancellationToken>()
                            )
                        );
                }
            }

            public class HealthTests : BaseContainerTests
            {
                [Theory]
                [InlineData(HealthStatus.Healthy)]
                [InlineData(HealthStatus.Running)]
                public async Task InitializeAsync_WhenContainerHealthIsPassing_ExpectInitializationToComplete(HealthStatus status)
                {
                    // Arrange
                    var gatewayMock = new Mock<IDockerGateway>();
                    gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(status);

                    var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });
                    _containers.Add(container);

                    // Act
                    // Assert
                    await container.InitializeAsync();
                }

                [Fact]
                public async Task InitializeAsync_WhenContainerBecomesHealthy_ExpectContainerIdToBeReturned()
                {
                    // Arrange
                    var gatewayMock = new Mock<IDockerGateway>();
                    gatewayMock.SetupSequence(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(HealthStatus.Unknown)
                        .ReturnsAsync(HealthStatus.Unknown)
                        .ReturnsAsync(HealthStatus.Healthy);

                    var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });
                    _containers.Add(container);

                    var stopwatch = new Stopwatch();

                    // Act
                    stopwatch.Start();

                    await container.InitializeAsync();

                    stopwatch.Stop();

                    // Assert
                    Assert.True(stopwatch.Elapsed > TimeSpan.FromMilliseconds(50));
                }

                [Theory]
                [InlineData(HealthStatus.Unknown)]
                [InlineData(HealthStatus.Unhealthy)]
                public async Task InitializeAsync_WhenContainerHealthNeverPassesBeforeProvidedTimeout_ExpectExceptionToBeThrown(HealthStatus status)
                {
                    // Arrange
                    var gatewayMock = new Mock<IDockerGateway>();
                    gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(status);

                    var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });
                    _containers.Add(container);

                    // Act
                    // Assert
                    await Assert.ThrowsAsync<OperationCanceledException>(() => container.InitializeAsync());
                }
            }
        }

        public class DisposeTests : BaseContainerTests
        {
            [Fact]
            public async Task DisposeAsync_WhenCalled_ExpectADockerRemoveCommandToBeExecuted()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });
                _containers.Add(container);

                // Act
                await container.DisposeAsync();

                // Assert
                gatewayMock.Verify(x => x.ContainerRemoveAsync(container.Id, It.IsAny<CancellationToken>()), Times.Once);
            }

            [Fact]
            public async Task DisposeAsync_WhenCalledWhileAnotherContainerIsRunning_ExpectOnlyTheCalledContainerToBeRemoved()
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.ContainerRunAsync("runningimage", string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("runningid");
                gatewayMock.Setup(x => x.ContainerRunAsync("disposingimage", string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("disposingid");
                gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var runningContainer = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image("runningimage"), new Command(string.Empty) });
                _containers.Add(runningContainer);
                await runningContainer.InitializeAsync();

                var disposingContainer = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image("disposingimage"), new Command(string.Empty) });
                _containers.Add(disposingContainer);
                await disposingContainer.InitializeAsync();

                // Act
                await disposingContainer.DisposeAsync();

                // Assert
                gatewayMock.Verify(x => x.ContainerRemoveAsync(disposingContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
                gatewayMock.Verify(x => x.ContainerRemoveAsync(runningContainer.Id, It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        public class FileTests : BaseContainerTests
        {
            [Theory]
            [InlineData("file/one", "destination/one", "testowner", "testpermissions")]
            [InlineData("two", "two", "owner", "permissions")]
            public async Task AddFile_WhenCalled_ExpectDetailsToBeForwardedToTheGateway(string hostFilename, string containerFilename, string owner, string permissions)
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });
                _containers.Add(container);

                var cancellationToken = new CancellationToken();

                // Act
                await container.AddFileAsync(hostFilename, containerFilename, owner, permissions, cancellationToken);

                // Assert
                gatewayMock.Verify(x => x.ContainerAddFileAsync(container.Id, hostFilename, containerFilename, owner, permissions, cancellationToken), Times.Once);
            }

            [Theory]
            [InlineData("destination/one")]
            [InlineData("two")]
            public async Task RemoveFile_WhenCalled_ExpectDetailsToBeForwardedToTheGateway(string containerFilename)
            {
                // Arrange
                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });
                _containers.Add(container);

                // Act
                await container.RemoveFileAsync(containerFilename, CancellationToken.None);

                // Assert
                gatewayMock.Verify(x => x.ContainerRemoveFileAsync(container.Id, containerFilename, It.IsAny<CancellationToken>()), Times.Once);
            }

            [Theory]
            [InlineData("destination/one", "testowner", "testpermissions")]
            [InlineData("two", "owner", "permissions")]
            public async Task CreateFile_WhenCalledWithAString_ExpectGatewayToBeCalledWithATemporaryFile(string containerFilename, string owner, string permissions)
            {
                // Arrange
                var fileContents = Guid.NewGuid().ToString();

                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });
                _containers.Add(container);

                var actualFilename = default(string);
                var actualContents = default(string);

                gatewayMock.Setup(x => x.ContainerAddFileAsync(container.Id, It.IsAny<string>(), containerFilename, owner, permissions, It.IsAny<CancellationToken>()))
                    .Callback<string, string, string, string, string, CancellationToken>((_, hostFilename, _, _, _, _) =>
                    {
                        actualFilename = hostFilename;
                        actualContents = File.ReadAllText(hostFilename);
                    });

                // Act
                await container.CreateFileAsync(fileContents, containerFilename, owner, permissions, CancellationToken.None);

                // Assert
                Assert.Equal(Path.GetDirectoryName(Path.GetTempPath()), Path.GetDirectoryName(actualFilename));
                Assert.False(File.Exists(actualFilename));
                Assert.Equal(fileContents, actualContents);
            }

            [Theory]
            [InlineData("destination/one", "testowner", "testpermissions")]
            [InlineData("two", "owner", "permissions")]
            public async Task CreateFile_WhenCalledWithAStream_ExpectGatewayToBeCalledWithATemporaryFile(string containerFilename, string owner, string permissions)
            {
                // Arrange
                var fileContents = Guid.NewGuid().ToString();
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContents));

                var gatewayMock = new Mock<IDockerGateway>();
                gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var container = new Container(gatewayMock.Object, Guid.Empty, new Attribute[] { new Image(string.Empty), new Command(string.Empty) });
                _containers.Add(container);

                var actualFilename = default(string);
                var actualContents = default(string);

                gatewayMock.Setup(x => x.ContainerAddFileAsync(container.Id, It.IsAny<string>(), containerFilename, owner, permissions, It.IsAny<CancellationToken>()))
                    .Callback<string, string, string, string, string, CancellationToken>((_, hostFilename, _, _, _, _) =>
                    {
                        actualFilename = hostFilename;
                        actualContents = File.ReadAllText(hostFilename);
                    });

                // Act
                await container.CreateFileAsync(stream, containerFilename, owner, permissions, CancellationToken.None);

                // Assert
                Assert.Equal(Path.GetDirectoryName(Path.GetTempPath()), Path.GetDirectoryName(actualFilename));
                Assert.False(File.Exists(actualFilename));
                Assert.Equal(fileContents, actualContents);
            }
        }
    }
}
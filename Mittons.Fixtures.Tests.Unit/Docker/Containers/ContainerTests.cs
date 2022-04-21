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
using Mittons.Fixtures.Models;
using Mittons.Fixtures.Resources;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers;

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
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(imageName), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(x => x.RunAsync(imageName, string.Empty, It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
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
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(command), new RunAttribute() });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(x => x.RunAsync(string.Empty, command, It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
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

                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(parsed);
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                Assert.Equal(parsed, container.IpAddress);
            }

            [Theory]
            [InlineData("test", "www.example.com")]
            [InlineData("other", "sftp.example.com")]
            public async Task InitializeAsync_WhenANetworkAliasDoesNotSpecifyExternalStatus_ExpectTheContainerToBeConnectedToAnInternalNetwork(string networkName, string alias)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(IPAddress.Any);
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var instanceId = Guid.NewGuid();

                var networkGatewayMock = new Mock<INetworkGateway>();

                var networkAlias = new NetworkAliasAttribute(networkName, alias);

                var attributes = new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute(), networkAlias };

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, instanceId, attributes);
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                networkGatewayMock.Verify(x => x.ConnectAsync($"{networkName}-{instanceId}", container.Id, alias, It.IsAny<CancellationToken>()), Times.Once);
            }

            [Theory]
            [InlineData("test", "www.example.com")]
            [InlineData("other", "sftp.example.com")]
            public async Task InitializeAsync_WhenANetworkAliasIsSpecifiedForAnInternalNetwork_ExpectTheContainerToBeConnectedToAnInternalNetwork(string networkName, string alias)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(IPAddress.Any);
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var instanceId = Guid.NewGuid();

                var networkGatewayMock = new Mock<INetworkGateway>();

                var networkAlias = new NetworkAliasAttribute(networkName, alias, false);

                var attributes = new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute(), networkAlias };

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, instanceId, attributes);
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                networkGatewayMock.Verify(x => x.ConnectAsync($"{networkName}-{instanceId}", container.Id, alias, It.IsAny<CancellationToken>()), Times.Once);
            }

            [Theory]
            [InlineData("test", "www.example.com")]
            [InlineData("other", "sftp.example.com")]
            public async Task InitializeAsync_WhenANetworkAliasIsSpecifiedForAnExternalNetwork_ExpectTheContainerToBeConnectedToAnExternalNetwork(string networkName, string alias)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(IPAddress.Any);
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var instanceId = Guid.NewGuid();

                var networkGatewayMock = new Mock<INetworkGateway>();

                var networkAlias = new NetworkAliasAttribute(networkName, alias, true);

                var attributes = new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute(), networkAlias };

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, instanceId, attributes);
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                networkGatewayMock.Verify(x => x.ConnectAsync($"{networkName}", container.Id, alias, It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        public class LabelTests : BaseContainerTests
        {
            [Fact]
            public async Task InitializeAsync_WhenCreatedWithARun_ExpectLabelsToBePassedToTheGateway()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var run = new RunAttribute();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), run });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => y.Name == "--label" && y.Value == $"mittons.fixtures.run.id={run.Id}")),
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
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(status);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                // Act
                // Assert
                await container.InitializeAsync(CancellationToken.None);
            }

            [Fact]
            public async Task InitializeAsync_WhenContainerBecomesHealthy_ExpectContainerIdToBeReturned()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.SetupSequence(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Unknown)
                    .ReturnsAsync(HealthStatus.Unknown)
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                var stopwatch = new Stopwatch();

                // Act
                stopwatch.Start();

                await container.InitializeAsync(CancellationToken.None);

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
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(status);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(110));

                // Act
                // Assert
                await Assert.ThrowsAsync<OperationCanceledException>(() => container.InitializeAsync(cancellationTokenSource.Token));
            }

            [Fact]
            public async Task InitializeAsync_WhenHealthChecksAreDisabled_ExpectTheDisabledFlagToBeApplied()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Disabled = true },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => y.Name == "--no-healthcheck" && y.Value == string.Empty)),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Fact]
            public async Task InitializeAsync_WhenHealthChecksAreDisabledAndOtherFieldsAreSet_ExpectOnlyNoHealthCheckToBeApplied()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute
                        {
                            Disabled = true,
                            Command = "test",
                            Interval = 1,
                            Timeout = 1,
                            StartPeriod = 1,
                            Retries = 1
                        },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x =>
                                x.Any(y => y.Name == "--no-healthcheck") &&
                                !x.Any(y => y.Name == "--health-cmd") &&
                                !x.Any(y => y.Name == "--health-interval") &&
                                !x.Any(y => y.Name == "--health-timeout") &&
                                !x.Any(y => y.Name == "--health-start-period") &&
                                !x.Any(y => y.Name == "--health-retries")
                            ),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData("ps aux || exit 1")]
            [InlineData("echo hello")]
            public async Task InitializeAsync_WhenHealthCheckCommandIsSet_ExpectHealthCmdToBeApplied(string command)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Command = command },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => y.Name == "--health-cmd" && y.Value == command)),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData(1)]
            [InlineData(8)]
            public async Task InitializeAsync_WhenHealthCheckIntervalIsSet_ExpectHealthIntervalToBeApplied(byte seconds)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Interval = seconds },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => y.Name == "--health-interval" && y.Value == $"{seconds}s")),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData(1)]
            [InlineData(8)]
            public async Task InitializeAsync_WhenHealthCheckTimeoutIsSet_ExpectHealthTimeoutToBeApplied(byte seconds)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Timeout = seconds },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => y.Name == "--health-timeout" && y.Value == $"{seconds}s")),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData(1)]
            [InlineData(8)]
            public async Task InitializeAsync_WhenHealthCheckStartPeriodIsSet_ExpectHealthCheckStartPeriodToBeApplied(byte seconds)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { StartPeriod = seconds },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => y.Name == "--health-start-period" && y.Value == $"{seconds}s")),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(20)]
            public async Task InitializeAsync_WhenHealthRetriesIsSet_ExpectHealthRetriesToBeApplied(byte retries)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Retries = retries },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => y.Name == "--health-retries" && y.Value == retries.ToString())),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Fact]
            public async Task InitializeAsync_WhenHealthDetailsAreNotSet_ExpectNoHealthParametersToBeApplied()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute(),
                        new RunAttribute()
                    });
                _containers.Add(container);

                var healthParameters = new[]
                {
                    "--no-healthcheck",
                    "--health-cmd",
                    "--health-interval",
                    "--health-timeout",
                    "--health-start-period",
                    "--health-retries",
                };

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => healthParameters.Contains(y.Name))),
                            It.IsAny<CancellationToken>()
                        ),
                        Times.Never
                    );
            }

            [Fact]
            public async Task InitializeAsync_WhenHealthDetailsAreSetToZero_ExpectNoHealthParametersToBeApplied()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Interval = 0, Retries = 0, StartPeriod = 0, Timeout = 0 },
                        new RunAttribute()
                    });
                _containers.Add(container);

                var healthParameters = new[]
                {
                    "--no-healthcheck",
                    "--health-cmd",
                    "--health-interval",
                    "--health-timeout",
                    "--health-start-period",
                    "--health-retries",
                };

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => healthParameters.Contains(y.Name))),
                            It.IsAny<CancellationToken>()
                        ),
                        Times.Never
                    );
            }

            [Fact]
            public async Task InitializeAsync_WhenNoHealthAttributeIsProvided_ExpectNoHealthParametersToBeApplied()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new RunAttribute()
                    });
                _containers.Add(container);

                var healthParameters = new[]
                {
                    "--no-healthcheck",
                    "--health-cmd",
                    "--health-interval",
                    "--health-timeout",
                    "--health-start-period",
                    "--health-retries",
                };

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<Option>>(x => x.Any(y => healthParameters.Contains(y.Name))),
                            It.IsAny<CancellationToken>()
                        ),
                        Times.Never
                    );
            }
        }
    }

    public class DisposeTests : BaseContainerTests
    {
        [Fact]
        public async Task DisposeAsync_WhenCalled_ExpectADockerRemoveCommandToBeExecuted()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            // Act
            await container.DisposeAsync();

            // Assert
            containerGatewayMock.Verify(x => x.RemoveAsync(container.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenCalledWhileAnotherContainerIsRunning_ExpectOnlyTheCalledContainerToBeRemoved()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.RunAsync("runningimage", string.Empty, It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("runningid");
            containerGatewayMock.Setup(x => x.RunAsync("disposingimage", string.Empty, It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("disposingid");
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var runningContainer = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute("runningimage"), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(runningContainer);
            await runningContainer.InitializeAsync(CancellationToken.None);

            var disposingContainer = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute("disposingimage"), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(disposingContainer);
            await disposingContainer.InitializeAsync(CancellationToken.None);

            // Act
            await disposingContainer.DisposeAsync();

            // Assert
            containerGatewayMock.Verify(x => x.RemoveAsync(disposingContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
            containerGatewayMock.Verify(x => x.RemoveAsync(runningContainer.Id, It.IsAny<CancellationToken>()), Times.Never);
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
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            var cancellationToken = new CancellationToken();

            // Act
            await container.AddFileAsync(hostFilename, containerFilename, owner, permissions, cancellationToken);

            // Assert
            containerGatewayMock.Verify(x => x.AddFileAsync(container.Id, hostFilename, containerFilename, owner, permissions, cancellationToken), Times.Once);
        }

        [Theory]
        [InlineData("destination/one")]
        [InlineData("two")]
        public async Task RemoveFile_WhenCalled_ExpectDetailsToBeForwardedToTheGateway(string containerFilename)
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            // Act
            await container.RemoveFileAsync(containerFilename, CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.RemoveFileAsync(container.Id, containerFilename, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("destination/one")]
        [InlineData("two")]
        public async Task EmptyDirectoryAsync_WhenCalled_ExpectDetailsToBeForwardedToTheGateway(string directory)
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            // Act
            await container.EmptyDirectoryAsync(directory, CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.EmptyDirectoryAsync(container.Id, directory, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("destination/one", "testowner", "testpermissions")]
        [InlineData("two", "owner", "permissions")]
        public async Task CreateFile_WhenCalledWithAString_ExpectGatewayToBeCalledWithATemporaryFile(string containerFilename, string owner, string permissions)
        {
            // Arrange
            var fileContents = Guid.NewGuid().ToString();

            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            var actualFilename = default(string);
            var actualContents = default(string);

            containerGatewayMock.Setup(x => x.AddFileAsync(container.Id, It.IsAny<string>(), containerFilename, owner, permissions, It.IsAny<CancellationToken>()))
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

            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            var actualFilename = default(string);
            var actualContents = default(string);

            containerGatewayMock.Setup(x => x.AddFileAsync(container.Id, It.IsAny<string>(), containerFilename, owner, permissions, It.IsAny<CancellationToken>()))
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Attributes;
using Mittons.Fixtures.Containers;
using Mittons.Fixtures.Containers.Attributes;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Fixtures;

public class GuestEnvironmentFixtureTests
{
    public class DisposeTests
    {
        [Run(true)]
        private class TeardownEnvironmentFixture : GuestEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public IContainerService? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public IContainerService? RedisContainer { get; set; }

            public TeardownEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory)
                : base(serviceGatewayFactory)
            {
            }
        }

        [Run(false)]
        private class KeepUpEnvironmentFixture : GuestEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public IContainerService? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public IContainerService? RedisContainer { get; set; }

            public KeepUpEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory)
                : base(serviceGatewayFactory)
            {
            }
        }

        [Fact]
        public async Task DisposeAsync_WhenServicesExistAndRunIsDefinedToTeardownOnComplete_ExpectServicesToBeRemoved()
        {
            // Arrange
            var mockServiceGateway = new Mock<IServiceGateway<IService>>();
            mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Mock.Of<IContainerService>());

            var mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
            mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
                .Returns(mockServiceGateway.Object);

            var fixture = new TeardownEnvironmentFixture(mockServiceGatewayFactory.Object);

            await fixture.InitializeAsync(CancellationToken.None);

            // Act
            await fixture.DisposeAsync();

            // Assert
            mockServiceGateway.Verify(x => x.RemoveServiceAsync(fixture.AlpineContainer, It.IsAny<CancellationToken>()), Times.Once);
            mockServiceGateway.Verify(x => x.RemoveServiceAsync(fixture.RedisContainer, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenServicesExistAndRunIsDefinedToNotTeardownOnComplete_ExpectServicesToNotBeRemoved()
        {
            // Arrange
            var mockServiceGateway = new Mock<IServiceGateway<IService>>();
            mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Mock.Of<IContainerService>());

            var mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
            mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
                .Returns(mockServiceGateway.Object);

            var fixture = new KeepUpEnvironmentFixture(mockServiceGatewayFactory.Object);

            await fixture.InitializeAsync(CancellationToken.None);

            // Act
            await fixture.DisposeAsync();

            // Assert
            mockServiceGateway.Verify(x => x.RemoveServiceAsync(It.IsAny<IContainerService>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class InitializeTests
    {
        [Network("network1")]
        [Network("network2")]
        private class RunNotSetEnvironmentFixture : GuestEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public IContainerService? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public IContainerService? RedisContainer { get; set; }

            public RunNotSetEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory)
                : base(serviceGatewayFactory)
            {
            }
        }

        [Run("test")]
        [Network("network1")]
        [Network("network2")]
        private class RunSetEnvironmentFixture : GuestEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public IContainerService? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            [Command("test command")]
            public IContainerService? RedisContainer { get; set; }

            public RunSetEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory)
                : base(serviceGatewayFactory)
            {
            }
        }

        private readonly string _buildId;

        private readonly string _releaseId;

        public InitializeTests()
        {
            _buildId = Guid.NewGuid().ToString();
            Environment.SetEnvironmentVariable("BUILD_BUILDID", _buildId);

            _releaseId = Guid.NewGuid().ToString();
            Environment.SetEnvironmentVariable("RELEASE_RELEASEID", _releaseId);
        }

        [Fact]
        public async Task InitializeAsync_WhenFixtureContainsServices_ExpectServicesToBeCreated()
        {
            // Arrange
            var mockServiceGateway = new Mock<IServiceGateway<IService>>();
            mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Mock.Of<IContainerService>());

            var mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
            mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
                .Returns(mockServiceGateway.Object);

            var fixture = new RunNotSetEnvironmentFixture(mockServiceGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(fixture.AlpineContainer);
            Assert.NotNull(fixture.RedisContainer);
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithoutRunDetails_ExpectServicesToUseDefaultRunDetails()
        {
            // Arrange
            var mockServiceGateway = new Mock<IServiceGateway<IService>>();
            mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Mock.Of<IContainerService>());

            var mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
            mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
                .Returns(mockServiceGateway.Object);

            var fixture = new RunNotSetEnvironmentFixture(mockServiceGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None);

            // Assert
            mockServiceGateway.Verify(
                    method => method.CreateServiceAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<RunAttribute>().Any(run => run.Id == RunAttribute.DefaultId)
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Exactly(2)
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenFixtureHasStaticRunDetails_ExpectServicesToUseStaticRunDetails()
        {
            // Arrange
            var mockServiceGateway = new Mock<IServiceGateway<IService>>();
            mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Mock.Of<IContainerService>());

            var mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
            mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
                .Returns(mockServiceGateway.Object);

            var fixture = new RunSetEnvironmentFixture(mockServiceGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None);

            // Assert
            mockServiceGateway.Verify(
                    method => method.CreateServiceAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<RunAttribute>().Any(run => run.Id == "test")
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Exactly(2)
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenFixtureHasExtraAttributes_ExpectServicesToBeCreatedWithoutTheExtraAttributes()
        {
            // Arrange
            var mockServiceGateway = new Mock<IServiceGateway<IService>>();
            mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Mock.Of<IContainerService>());

            var mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
            mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
                .Returns(mockServiceGateway.Object);

            var fixture = new RunSetEnvironmentFixture(mockServiceGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None);

            // Assert
            mockServiceGateway.Verify(
                    method => method.CreateServiceAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<NetworkAttribute>().Any()
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenServiceHasAttributes_ExpectServicesToBeWithAttributes()
        {
            // Arrange
            var mockServiceGateway = new Mock<IServiceGateway<IService>>();
            mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Mock.Of<IContainerService>());

            var mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
            mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
                .Returns(mockServiceGateway.Object);

            var fixture = new RunSetEnvironmentFixture(mockServiceGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None);

            // Assert
            mockServiceGateway.Verify(
                    method => method.CreateServiceAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<ImageAttribute>().Any(image => image.Name == "alpine:3.15")
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            mockServiceGateway.Verify(
                    method => method.CreateServiceAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<ImageAttribute>().Any(image => image.Name == "redis:alpine") && attribute.OfType<CommandAttribute>().Any(command => command.Value == "test command")
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }
    }

    // public class NetworkTests : IAsyncDisposable
    // {
    //     [Network("network1")]
    //     [Network("network2")]
    //     private class NetworkTestEnvironmentFixture : DockerEnvironmentFixture
    //     {
    //         [Image("alpine:3.15")]
    //         [NetworkAlias("network1", "alpine.example.com")]
    //         public Container? AlpineContainer { get; set; }

    //         [NetworkAlias("network1", "sftp.example.com")]
    //         [NetworkAlias("network2", "sftp-other.example.com")]
    //         public SftpContainer? SftpContainer { get; set; }

    //         public NetworkTestEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
    //             : base(containerGateway, networkGateway)
    //         {
    //         }
    //     }

    //     [Network("network1")]
    //     [Network("network1")]
    //     private class DuplicateNetworkTestEnvironmentFixture : DockerEnvironmentFixture
    //     {
    //         [Image("alpine:3.15")]
    //         public Container? GuestContainer { get; set; }

    //         public SftpContainer? AccountsContainer { get; set; }

    //         public DuplicateNetworkTestEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
    //             : base(containerGateway, networkGateway)
    //         {
    //         }
    //     }

    //     private readonly List<DockerEnvironmentFixture> _fixtures = new List<DockerEnvironmentFixture>();

    //     public async ValueTask DisposeAsync()
    //     {
    //         foreach (var fixture in _fixtures)
    //         {
    //             await fixture.DisposeAsync();
    //         }
    //     }

    //     [Fact]
    //     public async Task InitializeAsync_WhenNetworksAreDefinedForAFixture_ExpectTheNetworksToBeCreated()
    //     {
    //         // Arrange
    //         var containerGatewayMock = new Mock<IContainerGateway>();
    //         containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    //             .ReturnsAsync(HealthStatus.Healthy);

    //         var networkGatewayMock = new Mock<INetworkGateway>();

    //         var fixture = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
    //         _fixtures.Add(fixture);

    //         // Act
    //         await fixture.InitializeAsync();

    //         // Assert
    //         networkGatewayMock.Verify(x => x.CreateAsync($"network1-{fixture.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
    //         networkGatewayMock.Verify(x => x.CreateAsync($"network2-{fixture.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
    //     }

    //     [Fact]
    //     public void Ctor_WhenDuplicateNetworksAreDefinedForAFixture_ExpectAnErrorToBeThrown()
    //     {
    //         // Arrange
    //         var containerGatewayMock = new Mock<IContainerGateway>();
    //         containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    //             .ReturnsAsync(HealthStatus.Healthy);

    //         var networkGatewayMock = new Mock<INetworkGateway>();

    //         // Act
    //         // Assert
    //         Assert.Throws<NotSupportedException>(() => new DuplicateNetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object));
    //     }

    //     [Fact]
    //     public async Task InitializeAsync_WhenDuplicateNetworksAreCreatedForDifferentFixtures_ExpectTheNetworksToBeCreatedAndScopedToTheirFixture()
    //     {
    //         // Arrange
    //         var containerGatewayMock = new Mock<IContainerGateway>();
    //         containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    //             .ReturnsAsync(HealthStatus.Healthy);

    //         var networkGatewayMock = new Mock<INetworkGateway>();

    //         var fixture1 = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
    //         var fixture2 = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
    //         _fixtures.Add(fixture1);
    //         _fixtures.Add(fixture2);

    //         // Act
    //         await fixture1.InitializeAsync();
    //         await fixture2.InitializeAsync();

    //         // Assert
    //         networkGatewayMock.Verify(x => x.CreateAsync($"network1-{fixture1.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
    //         networkGatewayMock.Verify(x => x.CreateAsync($"network2-{fixture1.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
    //         networkGatewayMock.Verify(x => x.CreateAsync($"network1-{fixture2.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
    //         networkGatewayMock.Verify(x => x.CreateAsync($"network2-{fixture2.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
    //     }

    //     [Fact]
    //     public async Task InitializeAsync_WhenContainersHaveDefinedNetworkAliases_ExpectTheContainersToBeConnectedToTheDefinedNetworks()
    //     {
    //         // Arrange
    //         var containerGatewayMock = new Mock<IContainerGateway>();
    //         containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    //             .ReturnsAsync(HealthStatus.Healthy);

    //         var networkGatewayMock = new Mock<INetworkGateway>();

    //         var fixture = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
    //         _fixtures.Add(fixture);

    //         // Act
    //         await fixture.InitializeAsync();

    //         // Assert
    //         Assert.NotNull(fixture.AlpineContainer);
    //         Assert.NotNull(fixture.SftpContainer);

    //         if (fixture.AlpineContainer is null || fixture.SftpContainer is null)
    //         {
    //             return;
    //         }

    //         networkGatewayMock.Verify(x => x.ConnectAsync($"network1-{fixture.InstanceId}", fixture.AlpineContainer.Id, "alpine.example.com", It.IsAny<CancellationToken>()), Times.Once);
    //         networkGatewayMock.Verify(x => x.ConnectAsync($"network1-{fixture.InstanceId}", fixture.SftpContainer.Id, "sftp.example.com", It.IsAny<CancellationToken>()), Times.Once);
    //         networkGatewayMock.Verify(x => x.ConnectAsync($"network2-{fixture.InstanceId}", fixture.SftpContainer.Id, "sftp-other.example.com", It.IsAny<CancellationToken>()), Times.Once);
    //     }

    //     [Fact]
    //     public async Task DisposeAsync_WhenCalled_ExpectNetworksToBeRemoved()
    //     {
    //         // Arrange
    //         var containerGatewayMock = new Mock<IContainerGateway>();
    //         containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    //             .ReturnsAsync(HealthStatus.Healthy);

    //         var networkGatewayMock = new Mock<INetworkGateway>();

    //         var fixture = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
    //         _fixtures.Add(fixture);

    //         await fixture.InitializeAsync();

    //         // Act
    //         await fixture.DisposeAsync();

    //         // Assert
    //         networkGatewayMock.Verify(x => x.RemoveAsync($"network1-{fixture.InstanceId}", It.IsAny<CancellationToken>()), Times.Once);
    //         networkGatewayMock.Verify(x => x.RemoveAsync($"network2-{fixture.InstanceId}", It.IsAny<CancellationToken>()), Times.Once);
    //     }
    // }
}

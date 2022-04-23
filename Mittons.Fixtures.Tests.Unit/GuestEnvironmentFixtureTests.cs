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

            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Act
            await fixture.DisposeAsync().ConfigureAwait(false);

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

            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Act
            await fixture.DisposeAsync().ConfigureAwait(false);

            // Assert
            mockServiceGateway.Verify(x => x.RemoveServiceAsync(It.IsAny<IContainerService>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class InitializeTests
    {
        private class RunNotSetEnvironmentFixture : GuestEnvironmentFixture
        {
            [Network("network1")]
            public IContainerNetwork? Netowrk1 { get; set; }

            [Network("network2")]
            public IContainerNetwork? Network2 { get; set; }

            [Image("alpine:3.15")]
            public IContainerService? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public IContainerService? RedisContainer { get; set; }

            public RunNotSetEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory, INetworkGatewayFactory networkGatewayFactory)
                : base(serviceGatewayFactory, networkGatewayFactory)
            {
            }
        }

        [Run("test")]
        private class RunSetEnvironmentFixture : GuestEnvironmentFixture
        {
            [Network("network1")]
            public IContainerNetwork? Netowrk1 { get; set; }

            [Network("network2")]
            public IContainerNetwork? Network2 { get; set; }

            [Image("alpine:3.15")]
            public IContainerService? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            [Command("test command")]
            public IContainerService? RedisContainer { get; set; }

            public RunSetEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory, INetworkGatewayFactory networkGatewayFactory)
                : base(serviceGatewayFactory, networkGatewayFactory)
            {
            }
        }

        private readonly string _buildId;

        private readonly string _releaseId;

        private readonly Mock<IServiceGateway<IService>> _mockServiceGateway;

        private readonly Mock<IServiceGatewayFactory> _mockServiceGatewayFactory;

        private readonly Mock<INetworkGateway<INetwork>> _mockNetworkGateway;

        private readonly Mock<INetworkGatewayFactory> _mockNetworkGatewayFactory;

        public InitializeTests()
        {
            _buildId = Guid.NewGuid().ToString();
            Environment.SetEnvironmentVariable("BUILD_BUILDID", _buildId);

            _releaseId = Guid.NewGuid().ToString();
            Environment.SetEnvironmentVariable("RELEASE_RELEASEID", _releaseId);

            _mockServiceGateway = new Mock<IServiceGateway<IService>>();
            _mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Mock.Of<IContainerService>());

            _mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
            _mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
                .Returns(_mockServiceGateway.Object);

            _mockNetworkGateway = new Mock<INetworkGateway<INetwork>>();
            _mockNetworkGateway.Setup(x => x.CreateNetworkAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Mock.Of<IContainerNetwork>());

            _mockNetworkGatewayFactory = new Mock<INetworkGatewayFactory>();
            _mockNetworkGatewayFactory.Setup(x => x.GetNetworkGateway(It.IsAny<Type>()))
                .Returns(_mockNetworkGateway.Object);
        }

        [Fact]
        public async Task InitializeAsync_WhenFixtureContainsNetworks_ExpectNetworksToBeCreated()
        {
            // Arrange
            var fixture = new RunNotSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(fixture.Netowrk1);
            Assert.NotNull(fixture.Network2);
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithoutRunDetails_ExpectNetworksToUseDefaultRunDetails()
        {
            // Arrange
            var fixture = new RunNotSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockNetworkGateway.Verify(
                    method => method.CreateNetworkAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<RunAttribute>().Any(run => run.Id == RunAttribute.DefaultId)
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Exactly(2)
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenFixtureHasStaticRunDetails_ExpectNetworksToUseStaticRunDetails()
        {
            // Arrange
            var fixture = new RunSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockNetworkGateway.Verify(
                    method => method.CreateNetworkAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<RunAttribute>().Any(run => run.Id == "test")
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Exactly(2)
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenNetworksHaveAttributes_ExpectNetworksToBeWithAttributes()
        {
            // Arrange
            var fixture = new RunSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockNetworkGateway.Verify(
                    method => method.CreateNetworkAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<NetworkAttribute>().Any(network => network.Name == "network1")
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            _mockNetworkGateway.Verify(
                    method => method.CreateNetworkAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<NetworkAttribute>().Any(network => network.Name == "network2")
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenFixtureContainsServices_ExpectServicesToBeCreated()
        {
            // Arrange
            var fixture = new RunNotSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(fixture.AlpineContainer);
            Assert.NotNull(fixture.RedisContainer);
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithoutRunDetails_ExpectServicesToUseDefaultRunDetails()
        {
            // Arrange
            var fixture = new RunNotSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockServiceGateway.Verify(
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
            var fixture = new RunSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockServiceGateway.Verify(
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
        public async Task InitializeAsync_WhenServiceHasAttributes_ExpectServicesToBeWithAttributes()
        {
            // Arrange
            var fixture = new RunSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

            // Act
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockServiceGateway.Verify(
                    method => method.CreateServiceAsync(
                        It.Is<IEnumerable<Attribute>>(
                            attribute => attribute.OfType<ImageAttribute>().Any(image => image.Name == "alpine:3.15")
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            _mockServiceGateway.Verify(
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
}

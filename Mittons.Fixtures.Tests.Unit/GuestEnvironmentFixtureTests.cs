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
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

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
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

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
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

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
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

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
            await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

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
}

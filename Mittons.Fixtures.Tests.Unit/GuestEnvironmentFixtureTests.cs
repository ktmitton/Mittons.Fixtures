using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mittons.Fixtures.Attributes;
using Mittons.Fixtures.Containers;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit;

public class GuestEnvironmentFixtureTests
{
    public const string TestRunId = "Test Run";

    [Run(TestRunId, false)]
    private class DefinedRunGuestEnvironmentFixture : TestGuestEnvironmentFixture
    {
        public DefinedRunGuestEnvironmentFixture(bool clearRegistrations, bool addMockRegistrations) : base(clearRegistrations, addMockRegistrations)
        {
        }
    }

    private class TestGuestEnvironmentFixture : GuestEnvironmentFixture
    {
        [AllowNull]
        [Network("PrimaryNetwork")]
        public IContainerNetworkService PrimaryContainerNetwork { get; set; }

        public List<Mock<INetworkService>> Services { get; }

        public TestGuestEnvironmentFixture(bool clearRegistrations, bool addMockRegistrations)
            : base()
        {
            Services = new List<Mock<INetworkService>>();

            if (clearRegistrations)
            {
                base._serviceCollection.Clear();
            }

            if (addMockRegistrations)
            {
                base._serviceCollection.AddTransient<IContainerNetworkService>(
                    (_) =>
                    {
                        var mockNetwork = new Mock<IContainerNetworkService>();
                        mockNetwork.SetupGet(x => x.ServiceId)
                            .Returns($"Test-{Guid.NewGuid()}");

                        Services.Add(mockNetwork.As<INetworkService>());

                        return mockNetwork.Object;
                    });
            }

            base._serviceCollection.AddSingleton<IContainerNetworkGateway>(_ => Mock.Of<IContainerNetworkGateway>());
        }
    }

    public class RunTrackingTests
    {
        [Fact]
        public async Task InitializeAsync_WhenTheFixtureHasNoDefinedRunAttribute_ExpectCreatedServicesToUseADefaultRunAttribute()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: true);

            // Act
            await fixture.InitializeAsync(cancellationToken);

            // Assert
            foreach (var service in fixture.Services)
            {
                service.Verify(
                        x => x.InitializeAsync(
                            It.Is<IEnumerable<Attribute>>(
                                attributes => (attributes.OfType<RunAttribute>().Any(x => x.Id == RunAttribute.DefaultId && x.TeardownOnComplete) && attributes.OfType<RunAttribute>().Count() == 1)
                            ),
                            It.IsAny<CancellationToken>()),
                        Times.Once
                    );
            }
        }

        [Fact]
        public async Task InitializeAsync_WhenTheFixtureHasADefinedRunAttribute_ExpectCreatedServicesToUseTheRunAttribute()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var fixture = new DefinedRunGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: true);

            // Act
            await fixture.InitializeAsync(cancellationToken);

            // Assert
            foreach (var service in fixture.Services)
            {
                service.Verify(
                        x => x.InitializeAsync(
                            It.Is<IEnumerable<Attribute>>(
                                attributes => (attributes.OfType<RunAttribute>().Any(x => x.Id == TestRunId && !x.TeardownOnComplete) && attributes.OfType<RunAttribute>().Count() == 1)
                            ),
                            It.IsAny<CancellationToken>()),
                        Times.Once
                    );
            }
        }
    }

    public class LifetimeTests
    {
        [Fact]
        public async Task InitializeAsync_WhenAServiceIsCreated_ExpectTheServiceToBeIntitialized()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: true);

            // Act
            await fixture.InitializeAsync(cancellationToken);

            // Assert
            foreach (var service in fixture.Services)
            {
                service.Verify(x => x.InitializeAsync(It.IsAny<IEnumerable<Attribute>>(), cancellationToken), Times.Once);
            }
        }

        [Fact]
        public async Task DisposeAsync_WhenTheFixtureIsDisposed_ExpectAllOfItsServicesToBeDisposed()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: true);
            await fixture.InitializeAsync(cancellationToken);

            // Act
            await fixture.DisposeAsync();

            // Assert
            foreach (var service in fixture.Services)
            {
                service.Verify(x => x.DisposeAsync(), Times.Once);
            }
        }
    }

    public class ServiceProviderTests
    {
        [Fact]
        public async Task InitializeAsync_WhenAContainerNetworkIsRequestedWithDefaultRegistrations_ExpectNetworkToBeResolved()
        {
            // Arrange
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: false);

            // Act
            await fixture.InitializeAsync();

            // Assert
            Assert.NotNull(fixture.PrimaryContainerNetwork);
        }

        [Fact]
        public async Task InitializeAsync_WhenAContainerNetworkIsRequestedWithNoRegistrations_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: true, addMockRegistrations: false);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.InitializeAsync());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task InitializeAsync_WhenAContainerNetworkIsRequestedWithCustomRegistrations_ExpectNetworkToBeResolved(bool clearRegistrations)
        {
            // Arrange
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: clearRegistrations, addMockRegistrations: true);

            // Act
            await fixture.InitializeAsync();

            // Assert
            Assert.NotNull(fixture.PrimaryContainerNetwork);
            Assert.StartsWith("Test-", fixture.PrimaryContainerNetwork.ServiceId);
        }
    }
    //     public class DisposeTests
    //     {
    //         [Run(true)]
    //         private class TeardownEnvironmentFixture : GuestEnvironmentFixture
    //         {
    //             [Network("network1")]
    //             public IContainerNetwork? Netowrk1 { get; set; }

    //             [Network("network2")]
    //             public IContainerNetwork? Network2 { get; set; }

    //             [Image("alpine:3.15")]
    //             public IContainerService? AlpineContainer { get; set; }

    //             [Image("redis:alpine")]
    //             public IContainerService? RedisContainer { get; set; }

    //             public TeardownEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory, INetworkGatewayFactory networkGatewayFactory)
    //                 : base(serviceGatewayFactory, networkGatewayFactory)
    //             {
    //             }
    //         }

    //         [Run(false)]
    //         private class KeepUpEnvironmentFixture : GuestEnvironmentFixture
    //         {
    //             [Network("network1")]
    //             public IContainerNetwork? Netowrk1 { get; set; }

    //             [Network("network2")]
    //             public IContainerNetwork? Network2 { get; set; }

    //             [Image("alpine:3.15")]
    //             public IContainerService? AlpineContainer { get; set; }

    //             [Image("redis:alpine")]
    //             public IContainerService? RedisContainer { get; set; }

    //             public KeepUpEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory, INetworkGatewayFactory networkGatewayFactory)
    //                 : base(serviceGatewayFactory, networkGatewayFactory)
    //             {
    //             }
    //         }

    //         private readonly Mock<IServiceGateway<IService>> _mockServiceGateway;

    //         private readonly Mock<IServiceGatewayFactory> _mockServiceGatewayFactory;

    //         private readonly Mock<INetworkGateway<INetwork>> _mockNetworkGateway;

    //         private readonly Mock<INetworkGatewayFactory> _mockNetworkGatewayFactory;

    //         public DisposeTests()
    //         {
    //             _mockServiceGateway = new Mock<IServiceGateway<IService>>();
    //             _mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
    //                 .ReturnsAsync(() => Mock.Of<IContainerService>());

    //             _mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
    //             _mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
    //                 .Returns(_mockServiceGateway.Object);

    //             _mockNetworkGateway = new Mock<INetworkGateway<INetwork>>();
    //             _mockNetworkGateway.Setup(x => x.CreateNetworkAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
    //                 .ReturnsAsync(() => Mock.Of<IContainerNetwork>());

    //             _mockNetworkGatewayFactory = new Mock<INetworkGatewayFactory>();
    //             _mockNetworkGatewayFactory.Setup(x => x.GetNetworkGateway(It.IsAny<Type>()))
    //                 .Returns(_mockNetworkGateway.Object);
    //         }

    //         [Fact]
    //         public async Task DisposeAsync_WhenNetworksExistAndRunIsDefinedToTeardownOnComplete_ExpectNetworksToBeRemoved()
    //         {
    //             // Arrange
    //             var fixture = new TeardownEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Act
    //             await fixture.DisposeAsync().ConfigureAwait(false);

    //             // Assert
    // #nullable disable warnings
    //             _mockNetworkGateway.Verify(x => x.RemoveNetworkAsync(fixture.Netowrk1, It.IsAny<CancellationToken>()), Times.Once);
    //             _mockNetworkGateway.Verify(x => x.RemoveNetworkAsync(fixture.Network2, It.IsAny<CancellationToken>()), Times.Once);
    // #nullable enable warnings
    //         }

    //         [Fact]
    //         public async Task DisposeAsync_WhenNetworksExistAndRunIsDefinedToNotTeardownOnComplete_ExpectNetworksToNotBeRemoved()
    //         {
    //             // Arrange
    //             var fixture = new KeepUpEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Act
    //             await fixture.DisposeAsync().ConfigureAwait(false);

    //             // Assert
    //             _mockNetworkGateway.Verify(x => x.RemoveNetworkAsync(It.IsAny<INetwork>(), It.IsAny<CancellationToken>()), Times.Never);
    //         }

    //         [Fact]
    //         public async Task DisposeAsync_WhenServicesExistAndRunIsDefinedToTeardownOnComplete_ExpectServicesToBeRemoved()
    //         {
    //             // Arrange
    //             var fixture = new TeardownEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Act
    //             await fixture.DisposeAsync().ConfigureAwait(false);

    //             // Assert
    // #nullable disable warnings
    //             _mockServiceGateway.Verify(x => x.RemoveServiceAsync(fixture.AlpineContainer, It.IsAny<CancellationToken>()), Times.Once);
    //             _mockServiceGateway.Verify(x => x.RemoveServiceAsync(fixture.RedisContainer, It.IsAny<CancellationToken>()), Times.Once);
    // #nullable enable warnings
    //         }

    //         [Fact]
    //         public async Task DisposeAsync_WhenServicesExistAndRunIsDefinedToNotTeardownOnComplete_ExpectServicesToNotBeRemoved()
    //         {
    //             // Arrange
    //             var fixture = new KeepUpEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Act
    //             await fixture.DisposeAsync().ConfigureAwait(false);

    //             // Assert
    //             _mockServiceGateway.Verify(x => x.RemoveServiceAsync(It.IsAny<IContainerService>(), It.IsAny<CancellationToken>()), Times.Never);
    //         }
    //     }

    //     public class InitializeTests
    //     {
    //         private class RunNotSetEnvironmentFixture : GuestEnvironmentFixture
    //         {
    //             [Network("network1")]
    //             public IContainerNetwork? Netowrk1 { get; set; }

    //             [Network("network2")]
    //             public IContainerNetwork? Network2 { get; set; }

    //             [Image("alpine:3.15")]
    //             public IContainerService? AlpineContainer { get; set; }

    //             [Image("redis:alpine")]
    //             public IContainerService? RedisContainer { get; set; }

    //             public RunNotSetEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory, INetworkGatewayFactory networkGatewayFactory)
    //                 : base(serviceGatewayFactory, networkGatewayFactory)
    //             {
    //             }
    //         }

    //         [Run("test")]
    //         private class RunSetEnvironmentFixture : GuestEnvironmentFixture
    //         {
    //             [Network("network1")]
    //             public IContainerNetwork? Network1 { get; set; }

    //             [Network("network2")]
    //             public IContainerNetwork? Network2 { get; set; }

    //             [Image("alpine:3.15")]
    //             [NetworkAlias("network1", "alpine.example.com")]
    //             public IContainerService? AlpineContainer { get; set; }

    //             [Image("redis:alpine")]
    //             [Command("test command")]
    //             [NetworkAlias("network2", "redis.example.com", true)]
    //             public IContainerService? RedisContainer { get; set; }

    //             public RunSetEnvironmentFixture(IServiceGatewayFactory serviceGatewayFactory, INetworkGatewayFactory networkGatewayFactory)
    //                 : base(serviceGatewayFactory, networkGatewayFactory)
    //             {
    //             }
    //         }

    //         private readonly string _buildId;

    //         private readonly string _releaseId;

    //         private readonly Mock<IServiceGateway<IService>> _mockServiceGateway;

    //         private readonly Mock<IServiceGatewayFactory> _mockServiceGatewayFactory;

    //         private readonly Mock<INetworkGateway<INetwork>> _mockNetworkGateway;

    //         private readonly Mock<INetworkGatewayFactory> _mockNetworkGatewayFactory;

    //         public InitializeTests()
    //         {
    //             _buildId = Guid.NewGuid().ToString();
    //             Environment.SetEnvironmentVariable("BUILD_BUILDID", _buildId);

    //             _releaseId = Guid.NewGuid().ToString();
    //             Environment.SetEnvironmentVariable("RELEASE_RELEASEID", _releaseId);

    //             _mockServiceGateway = new Mock<IServiceGateway<IService>>();
    //             _mockServiceGateway.Setup(x => x.CreateServiceAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
    //                 .ReturnsAsync((IEnumerable<Attribute> attributes, CancellationToken cancellationToken) =>
    //                     {
    //                         var mockService = new Mock<IContainerService>();
    //                         mockService.SetupGet(x => x.ServiceId)
    //                             .Returns(Guid.NewGuid().ToString());

    //                         return mockService.Object;
    //                     }
    //                 );

    //             _mockServiceGatewayFactory = new Mock<IServiceGatewayFactory>();
    //             _mockServiceGatewayFactory.Setup(x => x.GetServiceGateway(It.IsAny<Type>()))
    //                 .Returns(_mockServiceGateway.Object);

    //             _mockNetworkGateway = new Mock<INetworkGateway<INetwork>>();
    //             _mockNetworkGateway.Setup(x => x.CreateNetworkAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
    //                 .ReturnsAsync((IEnumerable<Attribute> attributes, CancellationToken cancellationToken) =>
    //                     {
    //                         var mockNetwork = new Mock<IContainerNetwork>();
    //                         mockNetwork.SetupGet(x => x.Name)
    //                             .Returns(attributes.OfType<NetworkAttribute>().FirstOrDefault()?.Name ?? string.Empty);
    //                         mockNetwork.SetupGet(x => x.NetworkId)
    //                             .Returns(Guid.NewGuid().ToString());

    //                         return mockNetwork.Object;
    //                     }
    //                 );

    //             _mockNetworkGatewayFactory = new Mock<INetworkGatewayFactory>();
    //             _mockNetworkGatewayFactory.Setup(x => x.GetNetworkGateway(It.IsAny<Type>()))
    //                 .Returns(_mockNetworkGateway.Object);
    //         }

    //         [Fact]
    //         public async Task InitializeAsync_WhenFixtureContainsNetworks_ExpectNetworksToBeCreated()
    //         {
    //             // Arrange
    //             var fixture = new RunNotSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             // Act
    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Assert
    //             Assert.NotNull(fixture.Netowrk1);
    //             Assert.NotNull(fixture.Network2);
    //         }

    //         [Fact]
    //         public async Task InitializeAsync_WhenInitializedWithoutRunDetails_ExpectNetworksToUseDefaultRunDetails()
    //         {
    //             // Arrange
    //             var fixture = new RunNotSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             // Act
    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Assert
    //             _mockNetworkGateway.Verify(
    //                     method => method.CreateNetworkAsync(
    //                         It.Is<IEnumerable<Attribute>>(
    //                             attribute => attribute.OfType<RunAttribute>().Any(run => run.Id == RunAttribute.DefaultId)
    //                         ),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Exactly(2)
    //                 );
    //         }

    //         [Fact]
    //         public async Task InitializeAsync_WhenFixtureHasStaticRunDetails_ExpectNetworksToUseStaticRunDetails()
    //         {
    //             // Arrange
    //             var fixture = new RunSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             // Act
    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Assert
    //             _mockNetworkGateway.Verify(
    //                     method => method.CreateNetworkAsync(
    //                         It.Is<IEnumerable<Attribute>>(
    //                             attribute => attribute.OfType<RunAttribute>().Any(run => run.Id == "test")
    //                         ),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Exactly(2)
    //                 );
    //         }

    //         [Fact]
    //         public async Task InitializeAsync_WhenNetworksHaveAttributes_ExpectNetworksToBeWithAttributes()
    //         {
    //             // Arrange
    //             var fixture = new RunSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             // Act
    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Assert
    //             _mockNetworkGateway.Verify(
    //                     method => method.CreateNetworkAsync(
    //                         It.Is<IEnumerable<Attribute>>(
    //                             attribute => attribute.OfType<NetworkAttribute>().Any(network => network.Name == "network1")
    //                         ),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Once
    //                 );
    //             _mockNetworkGateway.Verify(
    //                     method => method.CreateNetworkAsync(
    //                         It.Is<IEnumerable<Attribute>>(
    //                             attribute => attribute.OfType<NetworkAttribute>().Any(network => network.Name == "network2")
    //                         ),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Once
    //                 );
    //         }

    //         [Fact]
    //         public async Task InitializeAsync_WhenFixtureContainsServices_ExpectServicesToBeCreated()
    //         {
    //             // Arrange
    //             var fixture = new RunNotSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             // Act
    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Assert
    //             Assert.NotNull(fixture.AlpineContainer);
    //             Assert.NotNull(fixture.RedisContainer);
    //         }

    //         [Fact]
    //         public async Task InitializeAsync_WhenInitializedWithoutRunDetails_ExpectServicesToUseDefaultRunDetails()
    //         {
    //             // Arrange
    //             var fixture = new RunNotSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             // Act
    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Assert
    //             _mockServiceGateway.Verify(
    //                     method => method.CreateServiceAsync(
    //                         It.Is<IEnumerable<Attribute>>(
    //                             attribute => attribute.OfType<RunAttribute>().Any(run => run.Id == RunAttribute.DefaultId)
    //                         ),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Exactly(2)
    //                 );
    //         }

    //         [Fact]
    //         public async Task InitializeAsync_WhenFixtureHasStaticRunDetails_ExpectServicesToUseStaticRunDetails()
    //         {
    //             // Arrange
    //             var fixture = new RunSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             // Act
    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Assert
    //             _mockServiceGateway.Verify(
    //                     method => method.CreateServiceAsync(
    //                         It.Is<IEnumerable<Attribute>>(
    //                             attribute => attribute.OfType<RunAttribute>().Any(run => run.Id == "test")
    //                         ),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Exactly(2)
    //                 );
    //         }

    //         [Fact]
    //         public async Task InitializeAsync_WhenServiceHasAttributes_ExpectServicesToBeWithAttributes()
    //         {
    //             // Arrange
    //             var fixture = new RunSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             // Act
    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Assert
    //             _mockServiceGateway.Verify(
    //                     method => method.CreateServiceAsync(
    //                         It.Is<IEnumerable<Attribute>>(
    //                             attribute => attribute.OfType<ImageAttribute>().Any(image => image.Name == "alpine:3.15")
    //                         ),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Once
    //                 );
    //             _mockServiceGateway.Verify(
    //                     method => method.CreateServiceAsync(
    //                         It.Is<IEnumerable<Attribute>>(
    //                             attribute => attribute.OfType<ImageAttribute>().Any(image => image.Name == "redis:alpine") && attribute.OfType<CommandAttribute>().Any(command => command.Value == "test command")
    //                         ),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Once
    //                 );
    //         }

    //         [Fact]
    //         public async Task InitializeAsync_WhenServiceHasANetworkAlias_ExpectServiceToBeConnectedToTheNetwork()
    //         {
    //             // Arrange
    //             var fixture = new RunSetEnvironmentFixture(_mockServiceGatewayFactory.Object, _mockNetworkGatewayFactory.Object);

    //             // Act
    //             await fixture.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

    //             // Assert
    // #nullable disable warnings
    //             _mockNetworkGateway.Verify(
    //                     method => method.ConnectServiceAsync(
    //                         It.Is<IContainerNetwork>(network => network.NetworkId == fixture.Network1.NetworkId),
    //                         It.Is<IContainerService>(container => container.ServiceId == fixture.AlpineContainer.ServiceId),
    //                         It.Is<NetworkAliasAttribute>(alias => alias.Alias == "alpine.example.com" && alias.IsExternalNetwork == false),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Once
    //                 );
    //             _mockNetworkGateway.Verify(
    //                     method => method.ConnectServiceAsync(
    //                         It.Is<IContainerNetwork>(network => network.NetworkId == fixture.Network2.NetworkId),
    //                         It.Is<IContainerService>(container => container.ServiceId == fixture.RedisContainer.ServiceId),
    //                         It.Is<NetworkAliasAttribute>(alias => alias.Alias == "redis.example.com" && alias.IsExternalNetwork == true),
    //                         It.IsAny<CancellationToken>()
    //                     ),
    //                     Times.Once
    //                 );
    // #nullable enable warnings
    //         }
    //     }
}

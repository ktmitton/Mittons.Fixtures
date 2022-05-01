using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Containers.Services;
using Mittons.Fixtures.Core;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Services;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Core;

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

    private class DuplicateNetworkGuestEnvironmentFixture : TestGuestEnvironmentFixture
    {
        [AllowNull]
        [Network("PrimaryNetwork")]
        public IContainerNetworkService TertiaryNetwork { get; set; }

        public DuplicateNetworkGuestEnvironmentFixture(bool clearRegistrations, bool addMockRegistrations) : base(clearRegistrations, addMockRegistrations)
        {
        }
    }

    private class InvalidNetworkedServiceGuestEnvironmentFixture : TestGuestEnvironmentFixture
    {
        [AllowNull]
        [NetworkAlias("TertiaryNetwork", "tertiary.example.com")]
        public IContainerService TertiaryContainer { get; set; }

        public InvalidNetworkedServiceGuestEnvironmentFixture(bool clearRegistrations, bool addMockRegistrations) : base(clearRegistrations, addMockRegistrations)
        {
        }
    }

    private class TestGuestEnvironmentFixture : GuestEnvironmentFixture
    {
        [AllowNull]
        [Network("PrimaryNetwork")]
        public IContainerNetworkService PrimaryContainerNetwork { get; set; }

        [AllowNull]
        [Network("SecondaryNetwork")]
        public IContainerNetworkService SecondaryContainerNetwork { get; set; }

        [AllowNull]
        [NetworkAlias("PrimaryNetwork", "secondary.example.com")]
        public IContainerService PrimaryContainer { get; set; }

        [AllowNull]
        [NetworkAlias("SecondaryNetwork", "secondary.example.com")]
        public IContainerService SecondaryContainer { get; set; }

        public List<Mock<IService>> Services { get; }

        public TestGuestEnvironmentFixture(bool clearRegistrations, bool addMockRegistrations)
            : base()
        {
            Services = new List<Mock<IService>>();

            if (clearRegistrations)
            {
                base._serviceCollection.Clear();
            }

            if (addMockRegistrations)
            {
                base._serviceCollection.AddTransient<IContainerNetworkService>(
                    (_) =>
                    {
                        var networkName = Guid.NewGuid().ToString();

                        var mockNetwork = new Mock<IContainerNetworkService>();
                        mockNetwork.Setup(x => x.InitializeAsync(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<CancellationToken>()))
                            .Callback<IEnumerable<Attribute>, CancellationToken>(
                                    (attributes, cancellationToken) =>
                                    {
                                        networkName = attributes.OfType<NetworkAttribute>().First().Name;
                                    }
                                );
                        mockNetwork.SetupGet(x => x.Name)
                            .Returns(() => networkName);

                        mockNetwork.SetupGet(x => x.ServiceId)
                            .Returns($"Test-{Guid.NewGuid()}");

                        Services.Add(mockNetwork.As<IService>());

                        return mockNetwork.Object;
                    });

                base._serviceCollection.AddTransient<IContainerService>(
                    (_) =>
                    {
                        var mockContainer = new Mock<IContainerService>();

                        Services.Add(mockContainer.As<IService>());

                        return mockContainer.Object;
                    });
            }

            base._serviceCollection.AddSingleton<IContainerNetworkGateway>(
                    _ =>
                    {
                        var mockGateway = new Mock<IContainerNetworkGateway>();
                        mockGateway.Setup(x => x.CreateNetworkAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(Guid.NewGuid().ToString());

                        return mockGateway.Object;
                    }
                );
            base._serviceCollection.AddSingleton<IContainerGateway>(
                    _ =>
                    {
                        var mockGateway = new Mock<IContainerGateway>();
                        mockGateway.Setup(x => x.CreateContainerAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(Guid.NewGuid().ToString());

                        return mockGateway.Object;
                    }
                );
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
        public async Task InitializeAsync_WhenAFixtureIsInitializedWithNoRegistrations_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: true, addMockRegistrations: false);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.InitializeAsync());
        }

        [Fact]
        public async Task InitializeAsync_WhenTheFixtureContainsANetworkServiceWithDefaultRegistrations_ExpectServiceToBeResolved()
        {
            // Arrange
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: false);

            // Act
            await fixture.InitializeAsync();

            // Assert
            Assert.NotNull(fixture.PrimaryContainerNetwork);
        }

        [Fact]
        public async Task InitializeAsync_WhenTheFixtureContainsANetworkService_ExpectServiceToBeInitialized()
        {
            // Arrange
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: false);

            // Act
            await fixture.InitializeAsync();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(fixture.PrimaryContainerNetwork.ServiceId));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task InitializeAsync_WhenAContainerNetworkServiceIsRequestedWithCustomRegistrations_ExpectServiceToBeResolved(bool clearRegistrations)
        {
            // Arrange
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: clearRegistrations, addMockRegistrations: true);

            // Act
            await fixture.InitializeAsync();

            // Assert
            Assert.NotNull(fixture.PrimaryContainerNetwork);
            Assert.StartsWith("Test-", fixture.PrimaryContainerNetwork.ServiceId);
        }

        [Fact]
        public async Task InitializeAsync_WhenTheFixtureContainsAContainerServiceWithDefaultRegistrations_ExpectServiceToBeResolved()
        {
            // Arrange
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: false);

            // Act
            await fixture.InitializeAsync();

            // Assert
            Assert.NotNull(fixture.PrimaryContainer);
        }

        [Fact]
        public async Task InitializeAsync_WhenTheFixtureContainsAContainerService_ExpectServiceToBeInitialized()
        {
            // Arrange
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: false);

            // Act
            await fixture.InitializeAsync();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(fixture.PrimaryContainer.ServiceId));
        }
    }

    public class NetworkTests
    {
        [Fact]
        public async Task InitializeAsync_WhenTwoNetworksHaveTheSameName_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var fixture = new DuplicateNetworkGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: true);

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => fixture.InitializeAsync(cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WhenANetworkedServiceIsCreatedForAValidNetwork_ExpectNetworkToBeSetInTheAttribute()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: true);

            // Act
            await fixture.InitializeAsync(cancellationToken);

            // Assert
            var primaryContainerService = fixture.Services.First(x => x.Object == fixture.PrimaryContainer);
            primaryContainerService.Verify(x => x.InitializeAsync(It.Is<IEnumerable<Attribute>>(attributes => attributes.OfType<NetworkAliasAttribute>().Any(alias => alias.NetworkService == fixture.PrimaryContainerNetwork)), It.IsAny<CancellationToken>()), Times.Once);

            var secondaryContainerService = fixture.Services.First(x => x.Object == fixture.SecondaryContainer);
            secondaryContainerService.Verify(x => x.InitializeAsync(It.Is<IEnumerable<Attribute>>(attributes => attributes.OfType<NetworkAliasAttribute>().Any(alias => alias.NetworkService == fixture.SecondaryContainerNetwork)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenANetworkedServiceIsCreated_ExpectServiceToBeSetInTheAttribute()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var fixture = new TestGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: true);

            // Act
            await fixture.InitializeAsync(cancellationToken);

            // Assert
            var primaryContainerService = fixture.Services.First(x => x.Object == fixture.PrimaryContainer);
            primaryContainerService.Verify(x => x.InitializeAsync(It.Is<IEnumerable<Attribute>>(attributes => attributes.OfType<NetworkAliasAttribute>().Any(alias => alias.ConnectedService == fixture.PrimaryContainer)), It.IsAny<CancellationToken>()), Times.Once);

            var secondaryContainerService = fixture.Services.First(x => x.Object == fixture.SecondaryContainer);
            secondaryContainerService.Verify(x => x.InitializeAsync(It.Is<IEnumerable<Attribute>>(attributes => attributes.OfType<NetworkAliasAttribute>().Any(alias => alias.ConnectedService == fixture.SecondaryContainer)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenANetworkedServiceIsCreatedForAnInvalidNetwork_ExpectAnErrorToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var fixture = new InvalidNetworkedServiceGuestEnvironmentFixture(clearRegistrations: false, addMockRegistrations: true);

            // Act
            // Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.InitializeAsync(cancellationToken));
        }
    }
}

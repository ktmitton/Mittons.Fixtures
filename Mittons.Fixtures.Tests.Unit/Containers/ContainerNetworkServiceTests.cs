using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Containers.Services;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Services;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Containers;

public class ContainerNetworkServiceTests
{
    public class RunTrackingTests
    {
        [Theory]
        [InlineData("Test Run")]
        [InlineData("Other")]
        public async Task InitializeAsync_WhenARunAttributeIsProvided_ExpectTheNetworkToBeTaggedWithTheProvidedRunId(string expectedRunId)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("Test"), new RunAttribute(expectedRunId) };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockNetworkGateway.Verify(
                    x => x.CreateNetworkAsync(
                        It.IsAny<string>(),
                        It.Is<Dictionary<string, string>>(y => y.Any(z => z.Key == "mittons.fixtures.run.id" && z.Value == expectedRunId)),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenNoRunAttributeIsProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = Enumerable.Empty<Attribute>();

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WhenMultipleRunAttributesAreProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("Test"), new RunAttribute("Run 1"), new RunAttribute("Run 2") };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task DisposeAsync_WhenRunShouldTearServicesDownAfterCompletion_ExpectNetworkToBeRemoved()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("Test"), new RunAttribute(true) };

            await service.InitializeAsync(attributes, cancellationToken);

            // Act
            await service.DisposeAsync();

            // Assert
            mockNetworkGateway.Verify(x => x.RemoveNetworkAsync(service.ServiceId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenRunShouldNotTearServicesDownAfterCompletion_ExpectNetworkToNotBeRemoved()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("Test"), new RunAttribute(false) };

            await service.InitializeAsync(attributes, cancellationToken);

            // Act
            await service.DisposeAsync();

            // Assert
            mockNetworkGateway.Verify(x => x.RemoveNetworkAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class LifetimeTests
    {
        [Theory]
        [InlineData("NetworkId")]
        [InlineData("Other")]
        public async Task InitializeAsync_WhenTheNetworkIsCreated_ExpectTheServiceIdToBeSetToTheNetworkId(string expectedNetworkId)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();
            mockNetworkGateway.Setup(x => x.CreateNetworkAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedNetworkId);

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("Test"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            Assert.Equal(expectedNetworkId, service.ServiceId);
        }

        [Fact]
        public async Task InitializeAsync_WhenTheNetworkIsCreated_ExpectCancellationTokenToBePassedToTheGateway()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("Test"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockNetworkGateway.Verify(x => x.CreateNetworkAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenNoNetworkAttributeIsProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new RunAttribute() };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WhenMultipleNetworkAttributesAreProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("Test1"), new NetworkAttribute("Test2"), new RunAttribute() };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Theory]
        [InlineData(default(string))]
        [InlineData("")]
        [InlineData("   ")]
        public async Task InitializeAsync_WhenTheNetworkNameIsNull_ExpectAnExceptionToBeThrown(string networkName)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute(networkName), new RunAttribute() };

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Theory]
        [InlineData("Network1")]
        [InlineData("Other")]
        public async Task InitializeAsync_WhenTheNetworkNameIsValid_ExpectTheNetworkToBeCreatedWithTheName(string networkName)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute(networkName), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockNetworkGateway.Verify(x => x.CreateNetworkAsync(networkName, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("Network1")]
        [InlineData("Other")]
        public async Task InitializeAsync_WhenTheNetworkNameIsValid_ExpectTheNamePropertyIsSet(string networkName)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute(networkName), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            Assert.Equal(networkName, service.Name);
        }
    }

    public class ConnectedServiceTests
    {
        [Fact]
        public async Task ConnectAsync_WhenConnectingAnIncompatibleService_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockService = new Mock<IService>();

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var network = new ContainerNetworkService(mockNetworkGateway.Object);

            var networkAliasAttribute = new NetworkAliasAttribute("PrimaryNetwork", "primary.example.com")
            {
                NetworkService = network,
                ConnectedService = mockService.Object
            };

            // Act
            // Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => network.ConnectAsync(networkAliasAttribute, cancellationToken));
        }

        [Theory]
        [InlineData("Service1", "Network1", "primary.example.com")]
        [InlineData("SecondaryService", "SecondaryNetwork", "secondary.example.com")]
        public async Task ConnectAsync_WhenConnectingACompatibleService_ExpectTheServiceToBeConnected(string expectedContainerId, string expectedNetworkId, string expectedAlias)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockService = Mock.Of<IContainerService>(x => x.ServiceId == expectedContainerId);

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();
            mockNetworkGateway.Setup(x => x.CreateNetworkAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedNetworkId);

            var network = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("PrimaryNetwork"), new RunAttribute() };

            await network.InitializeAsync(attributes, cancellationToken);

            var networkAliasAttribute = new NetworkAliasAttribute("PrimaryNetwork", expectedAlias)
            {
                NetworkService = network,
                ConnectedService = mockService
            };

            // Act
            await network.ConnectAsync(networkAliasAttribute, cancellationToken);

            // Assert
            mockNetworkGateway.Verify(x => x.ConnectAsync(expectedNetworkId, expectedContainerId, expectedAlias, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ConnectAsync_WhenConnectingACompatibleService_ExpectTheProvidedCancellationTokenToBeUsed()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockService = Mock.Of<IContainerService>();

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var network = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("PrimaryNetwork"), new RunAttribute() };

            await network.InitializeAsync(attributes, cancellationToken);

            var networkAliasAttribute = new NetworkAliasAttribute("PrimaryNetwork", string.Empty)
            {
                NetworkService = network,
                ConnectedService = mockService
            };

            // Act
            await network.ConnectAsync(networkAliasAttribute, cancellationToken);

            // Assert
            mockNetworkGateway.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cancellationToken), Times.Once);
        }
    }

    public class ResourceTests
    {
        [Fact]
        public async Task InitializeAsync_WhenServiceIsInitialized_ExpectResourcesToBeEmpty()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockNetworkGateway = new Mock<IContainerNetworkGateway>();

            var service = new ContainerNetworkService(mockNetworkGateway.Object);

            var attributes = new Attribute[] { new NetworkAttribute("PrimaryNetwork"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            Assert.Empty(service.Resources);
        }
    }
}

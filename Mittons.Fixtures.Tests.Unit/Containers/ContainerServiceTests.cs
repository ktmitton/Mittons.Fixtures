using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Containers.Services;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Resources;
using Mittons.Fixtures.Core.Services;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Containers;

public class ContainerServiceTests
{
    public class RunTrackingTests
    {
        [Theory]
        [InlineData("Test Run")]
        [InlineData("Other")]
        public async Task InitializeAsync_WhenARunAttributeIsProvided_ExpectTheServiceToBeTaggedWithTheProvidedRunId(string expectedRunId)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(expectedRunId) };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(
                    x => x.CreateContainerAsync(
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

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage") };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WhenMultipleRunAttributesAreProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute("Run 1"), new RunAttribute("Run 2") };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task DisposeAsync_WhenRunShouldTearServicesDownAfterCompletion_ExpectServiceToBeRemoved()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(true) };

            await service.InitializeAsync(attributes, cancellationToken);

            // Act
            await service.DisposeAsync();

            // Assert
            mockContainerGateway.Verify(x => x.RemoveContainerAsync(service.ServiceId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenRunShouldNotTearServicesDownAfterCompletion_ExpectServiceToNotBeRemoved()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(false) };

            await service.InitializeAsync(attributes, cancellationToken);

            // Act
            await service.DisposeAsync();

            // Assert
            mockContainerGateway.Verify(x => x.RemoveContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class LifetimeTests
    {
        [Theory]
        [InlineData("ContainerId")]
        [InlineData("Other")]
        public async Task InitializeAsync_WhenTheContainerIsCreated_ExpectTheServiceIdToBeSetToTheContainerId(string expectedContainerId)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();
            mockContainerGateway.Setup(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedContainerId);

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            Assert.Equal(expectedContainerId, service.ServiceId);
        }

        [Theory]
        [InlineData("TestImage")]
        [InlineData("Image2")]
        public async Task InitializeAsync_WhenAnImageAttributeIsProvided_ExpectTheServiceToBeTaggedCreatedForTheImage(string expectedImageName)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute(expectedImageName), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(
                    x => x.CreateContainerAsync(
                        expectedImageName,
                        It.IsAny<Dictionary<string, string>>(),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenNoImageAttributeIsProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new RunAttribute() };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WhenMultipleImageAttributesAreProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("Image1"), new ImageAttribute("Image2"), new RunAttribute() };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WhenTheContainerIsCreated_ExpectCancellationTokenToBePassedToTheGateway()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), cancellationToken), Times.Once);
        }
    }

    public class ConnectedServiceTests
    {
        [Fact]
        public async Task InitializeAsync_WhenTheContainerHasANetworkAlias_ExpectTheContainerToBeConnectedToTheNetwork()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var mockNetwork = new Mock<INetworkService>();

            var networkAliasAttribute = new NetworkAliasAttribute("PrimaryNetwork", "primary.example.com")
            {
                NetworkService = mockNetwork.Object,
                ConnectedService = service
            };

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(), networkAliasAttribute };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockNetwork.Verify(x => x.ConnectAsync(networkAliasAttribute, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenTheContainerHasANetworkAlias_ExpectTheProvidedCancellationTokenToBeUsed()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var mockNetwork = new Mock<INetworkService>();

            var networkAliasAttribute = new NetworkAliasAttribute("PrimaryNetwork", "primary.example.com")
            {
                NetworkService = mockNetwork.Object,
                ConnectedService = service
            };

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(), networkAliasAttribute };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockNetwork.Verify(x => x.ConnectAsync(It.IsAny<NetworkAliasAttribute>(), cancellationToken), Times.Once);
        }
    }

    public class ResourceTests
    {
        [Fact]
        public async Task InitializeAsync_WhenServiceIsInitialized_ExpectResourcesToBeSet()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var expectedResources = new List<IResource> { Mock.Of<IResource>() };

            var mockContainerGateway = new Mock<IContainerGateway>();
            mockContainerGateway.Setup(x => x.GetAvailableResourcesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResources);

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            Assert.Equal(expectedResources, service.Resources);
        }

        [Fact]
        public async Task InitializeAsync_WhenGettingResources_ExpectTheProvidedCancellationTokenToBeUsed()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var expectedResources = new List<IResource> { Mock.Of<IResource>() };

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.GetAvailableResourcesAsync(It.IsAny<string>(), cancellationToken));
        }
    }
}

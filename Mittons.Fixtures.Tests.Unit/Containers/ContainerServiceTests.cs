using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Attributes;
using Mittons.Fixtures.Containers;
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

            var attributes = new Attribute[] { new RunAttribute(expectedRunId) };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(
                    x => x.CreateContainerAsync(
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

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new RunAttribute("Run 1"), new RunAttribute("Run 2") };

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

            var attributes = new Attribute[] { new RunAttribute(true) };

            await service.InitializeAsync(attributes, cancellationToken);

            // Act
            await service.DisposeAsync();

            // Assert
            mockContainerGateway.Verify(x => x.RemoveContainerAsync(service.ServiceId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenRunShouldNotTearServicesDownAfterCompletion_ExpectNetworkToNotBeRemoved()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new RunAttribute(false) };

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
            mockContainerGateway.Setup(x => x.CreateContainerAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedContainerId);

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            Assert.Equal(expectedContainerId, service.ServiceId);
        }

        [Fact]
        public async Task InitializeAsync_WhenTheContainerIsCreated_ExpectCancellationTokenToBePassedToTheGateway()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.CreateContainerAsync(It.IsAny<Dictionary<string, string>>(), cancellationToken), Times.Once);
        }
    }
}

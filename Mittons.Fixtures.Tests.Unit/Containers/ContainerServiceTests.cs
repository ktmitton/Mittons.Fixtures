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
    public class BuildTests
    {
        [Theory]
        [InlineData("path", "target", false, "image", "context", "arguments")]
        [InlineData("other path", "other target", true, "other image", "other context", "other arguments")]
        public async Task InitializeAsync_WhenABuildAttributeIsProvided_ExpectTheImageToBeBuilt(string dockerfilePath, string target, bool pullDependencyImages, string imageName, string context, string arguments)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new RunAttribute(), new ImageAttribute(imageName), new BuildAttribute(dockerfilePath, target, pullDependencyImages, context, arguments) };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(
                    x => x.BuildImageAsync(
                        dockerfilePath,
                        target,
                        pullDependencyImages,
                        imageName,
                        context,
                        arguments,
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenNoBuildAttributeIsProvided_ExpectTheImageToNotBeBuilt()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new RunAttribute(), new ImageAttribute("Image1") };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(
                    x => x.BuildImageAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenMultipleBuildAttributesAreProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new RunAttribute(), new ImageAttribute("Image1"), new BuildAttribute(string.Empty, string.Empty, false, string.Empty, string.Empty), new BuildAttribute(string.Empty, string.Empty, false, string.Empty, string.Empty) };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }
    }

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
                        It.IsAny<PullOption>(),
                        It.Is<Dictionary<string, string>>(y => y.Any(z => z.Key == "mittons.fixtures.run.id" && z.Value == expectedRunId)),
                        It.IsAny<Dictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IHealthCheckDescription>(),
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
            mockContainerGateway.Setup(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<PullOption>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IHealthCheckDescription>(), It.IsAny<CancellationToken>()))
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
        public async Task InitializeAsync_WhenAnImageAttributeIsProvided_ExpectTheServiceToBeCreatedForTheImage(string expectedImageName)
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
                        It.IsAny<PullOption>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IHealthCheckDescription>(),
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
            mockContainerGateway.Verify(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<PullOption>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IHealthCheckDescription>(), cancellationToken), Times.Once);
        }

        [Theory]
        [InlineData(PullOption.Always)]
        [InlineData(PullOption.Missing)]
        [InlineData(PullOption.Never)]
        public async Task InitializeAsync_WhenTheContainerIsCreated_ExpectThePullOptionToBeApplied(PullOption pullOption)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage", pullOption), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.CreateContainerAsync(It.IsAny<string>(), pullOption, It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IHealthCheckDescription>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenTheServiceIsDisposed_ExpectTheContainerToBeStopped()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();
            mockContainerGateway.Setup(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<PullOption>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IHealthCheckDescription>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("TestService");

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            await service.InitializeAsync(attributes, cancellationToken);

            // Act
            await service.DisposeAsync();

            // Assert
            mockContainerGateway.Verify(x => x.RemoveContainerAsync(service.ServiceId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class EnvironmentVariableTests
    {
        [Fact]
        public async Task InitializeAsync_WhenTheContainerHasEnvironmentVariablesDefined_ExpectTheContainerToStartWithTheVariables()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var mockNetwork = new Mock<INetworkService>();

            var attributes = new Attribute[]
            {
                new ImageAttribute("TestImage"),
                new RunAttribute(),
                new EnvironmentVariableAttribute("key1", "value1"),
                new EnvironmentVariableAttribute("key2", "value2")
            };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(
                    x =>
                    x.CreateContainerAsync(
                        It.IsAny<string>(),
                        It.IsAny<PullOption>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.Is<Dictionary<string, string>>(y => y["key1"] == "value1" && y["key2"] == "value2"),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IHealthCheckDescription>(),
                        It.IsAny<CancellationToken>()
                    )
                );
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

    public class CommandTests
    {
        [Theory]
        [InlineData("echo hello")]
        [InlineData("echo goodbye")]
        public async Task InitializeAsync_WhenACommandIsProvided_ExpectTheContainerToBeCreatedWithTheCommand(string expectedCommand)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(), new CommandAttribute(expectedCommand) };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<PullOption>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), expectedCommand, It.IsAny<IHealthCheckDescription>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task InitializeAsync_WhenMultipleCommandsAreProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(), new CommandAttribute("echo hello"), new CommandAttribute("echo goodbye") };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WhenNoCommandIsProvided_ExpectTheContainerToBeCreatedWithoutACommand()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<PullOption>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), default(string), It.IsAny<IHealthCheckDescription>(), It.IsAny<CancellationToken>()));
        }
    }

    public class HostnameTests
    {
        [Theory]
        [InlineData("host1")]
        [InlineData("other-hostname")]
        public async Task InitializeAsync_WhenAHostnameIsProvided_ExpectTheContainerToBeCreatedWithTheHostname(string expectedHostname)
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(), new HostnameAttribute(expectedHostname) };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<PullOption>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), expectedHostname, It.IsAny<string>(), It.IsAny<IHealthCheckDescription>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task InitializeAsync_WhenMultipleCommandsAreProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(), new HostnameAttribute("host1"), new HostnameAttribute("other-hostname") };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WhenNoHostnameIsProvided_ExpectTheContainerToBeCreatedWithoutAHostname()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<PullOption>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), default(string), It.IsAny<string>(), It.IsAny<IHealthCheckDescription>(), It.IsAny<CancellationToken>()));
        }
    }

    public class HealthCheckTests
    {
        [Theory]
        [InlineData(false, "command", 1, 2, 3, 4)]
        [InlineData(false, "other", 4, 3, 2, 1)]
        [InlineData(true, "", 0, 0, 0, 0)]
        public async Task InitializeAsync_WhenAHealthCheckIsProvided_ExpectTheContainerToBeCreatedWithTheHealthCheckParameters(
            bool expectedDisabled,
            string expectedCommand,
            byte expectedInterval,
            byte expectedTimeout,
            byte expectedStartPeriod,
            byte expectedRetries
        )
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var healthCheckAttribute = new HealthCheckAttribute
            {
                Disabled = expectedDisabled,
                Command = expectedCommand,
                Interval = expectedInterval,
                Timeout = expectedTimeout,
                StartPeriod = expectedStartPeriod,
                Retries = expectedRetries
            };

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(), healthCheckAttribute };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(
                    x =>
                    x.CreateContainerAsync(
                        It.IsAny<string>(),
                        It.IsAny<PullOption>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.Is<IHealthCheckDescription>(
                            y =>
                            y.Disabled == expectedDisabled &&
                            y.Command == expectedCommand &&
                            y.Interval == expectedInterval &&
                            y.Timeout == expectedTimeout &&
                            y.StartPeriod == expectedStartPeriod &&
                            y.Retries == expectedRetries
                        ),
                        It.IsAny<CancellationToken>()
                    )
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenMultipleHealthChecksAreProvided_ExpectAnExceptionToBeThrown()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute(), new HealthCheckAttribute(), new HealthCheckAttribute() };

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(attributes, cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WhenNoHealthChecksAreProvided_ExpectTheContainerToBeCreatedWithoutACommand()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<PullOption>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(), default(IHealthCheckDescription), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task InitializeAsync_WhenCalled_ExpectCheckForPassingHealthCheck()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.EnsureContainerIsHealthyAsync(service.ServiceId, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task InitializeAsync_WhenCheckingForAPassingHealthCheck_ExpectTheCancellationTokenToBePassedIn()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var mockContainerGateway = new Mock<IContainerGateway>();

            var service = new ContainerService(mockContainerGateway.Object);

            var attributes = new Attribute[] { new ImageAttribute("TestImage"), new RunAttribute() };

            // Act
            await service.InitializeAsync(attributes, cancellationToken);

            // Assert
            mockContainerGateway.Verify(x => x.EnsureContainerIsHealthyAsync(It.IsAny<string>(), cancellationToken));
        }
    }
}

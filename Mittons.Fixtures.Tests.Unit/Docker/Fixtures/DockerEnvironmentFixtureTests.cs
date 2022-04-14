using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Fixtures;
using Mittons.Fixtures.Docker.Gateways;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Environments;

public class DockerEnvironmentFixtureTests
{
    public class RunTests : IAsyncDisposable
    {
        [Run("${BUILD_BUILDID}")]
        [Network("network1")]
        [Network("network2")]
        private class BuildEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public Container? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public Container? RedisContainer { get; set; }

            public BuildEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        [Run("${RELEASE_RELEASEID}")]
        [Network("network1")]
        [Network("network2")]
        private class ReleaseEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public Container? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public Container? RedisContainer { get; set; }

            public ReleaseEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        [Run("${UNSET_UNSETID}")]
        [Network("network1")]
        [Network("network2")]
        private class UnsetEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public Container? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public Container? RedisContainer { get; set; }

            public UnsetEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        [Network("network1")]
        [Network("network2")]
        private class MissingEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public Container? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public Container? RedisContainer { get; set; }

            public MissingEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        [Run("test")]
        [Network("network1")]
        [Network("network2")]
        private class StaticEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public Container? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public Container? RedisContainer { get; set; }

            public StaticEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        private readonly string _buildId;

        private readonly string _releaseId;

        private readonly List<DockerEnvironmentFixture> _fixtures = new List<DockerEnvironmentFixture>();

        public async ValueTask DisposeAsync()
        {
            foreach (var fixture in _fixtures)
            {
                await fixture.DisposeAsync();
            }
        }

        public RunTests()
        {
            _buildId = Guid.NewGuid().ToString();
            Environment.SetEnvironmentVariable("BUILD_BUILDID", _buildId);

            _releaseId = Guid.NewGuid().ToString();
            Environment.SetEnvironmentVariable("RELEASE_RELEASEID", _releaseId);
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithRunDetailsFromBuildId_ExpectTheRunIdToBeSet()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new BuildEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={_buildId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={_buildId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={_buildId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={_buildId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithRunDetailsFromReleaseId_ExpectTheRunIdToBeSet()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new ReleaseEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={_releaseId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={_releaseId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={_releaseId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={_releaseId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithRunDetailsFromUnsetEnvironmentVariables_ExpectTheRunIdToBeDefault()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new UnsetEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={Run.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={Run.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={Run.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={Run.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithoutRunDetails_ExpectTheRunIdToBeDefault()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new MissingEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            // Act;
            await fixture.InitializeAsync();

            // Assert
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={Run.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={Run.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={Run.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id={Run.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithRunDetailsFromAStaticString_ExpectTheRunIdToBeSet()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new StaticEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id=test")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.NetworkCreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id=test")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id=test")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            gatewayMock.Verify(
                    x => x.ContainerRunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<KeyValuePair<string, string>>>(y => y.Any(z => z.Key == "--label" && z.Value == $"mittons.fixtures.run.id=test")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }
    }

    public class DisposeAsyncTests : IAsyncDisposable
    {
        [Run("test", false)]
        [Network("network1")]
        [Network("network2")]
        private class KeepUpWithIdEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public Container? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public Container? RedisContainer { get; set; }

            public KeepUpWithIdEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        [Run(false)]
        [Network("network1")]
        [Network("network2")]
        private class KeepUpWithoutIdEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public Container? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public Container? RedisContainer { get; set; }

            public KeepUpWithoutIdEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        private readonly List<DockerEnvironmentFixture> _fixtures = new List<DockerEnvironmentFixture>();

        public async ValueTask DisposeAsync()
        {
            foreach (var fixture in _fixtures)
            {
                await fixture.DisposeAsync();
            }
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithIdAndShouldKeepUpAfterComplete_ExpectTheEnvironmentToNotBeDisposed()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new KeepUpWithIdEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            await fixture.InitializeAsync();

            // Act
            await fixture.DisposeAsync();

            // Assert
            gatewayMock.Verify(
                    x => x.NetworkRemoveAsync(
                        $"network1-{fixture.InstanceId}",
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );
            gatewayMock.Verify(
                    x => x.NetworkRemoveAsync(
                        $"network2-{fixture.InstanceId}",
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );

            var alpineId = fixture.AlpineContainer?.Id ?? string.Empty;
            gatewayMock.Verify(
                    x => x.ContainerRemoveAsync(
                        alpineId,
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );

            var redisId = fixture.RedisContainer?.Id ?? string.Empty;
            gatewayMock.Verify(
                    x => x.ContainerRemoveAsync(
                        redisId,
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedAndWithNoIdShouldKeepUpAfterComplete_ExpectTheEnvironmentToNotBeDisposed()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new KeepUpWithoutIdEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            await fixture.InitializeAsync();

            // Act
            await fixture.DisposeAsync();

            // Assert
            gatewayMock.Verify(
                    x => x.NetworkRemoveAsync(
                        $"network1-{fixture.InstanceId}",
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );
            gatewayMock.Verify(
                    x => x.NetworkRemoveAsync(
                        $"network2-{fixture.InstanceId}",
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );

            var alpineId = fixture.AlpineContainer?.Id ?? string.Empty;
            gatewayMock.Verify(
                    x => x.ContainerRemoveAsync(
                        alpineId,
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );

            var redisId = fixture.RedisContainer?.Id ?? string.Empty;
            gatewayMock.Verify(
                    x => x.ContainerRemoveAsync(
                        redisId,
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );
        }
    }

    public class ContainerTests : IAsyncDisposable
    {
        private class ContainerTestEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public Container? AlpineContainer { get; set; }

            [Image("redis:alpine")]
            public Container? RedisContainer { get; set; }

            public ContainerTestEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        private readonly List<DockerEnvironmentFixture> _fixtures = new List<DockerEnvironmentFixture>();

        public async ValueTask DisposeAsync()
        {
            foreach (var fixture in _fixtures)
            {
                await fixture.DisposeAsync();
            }
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithContainerDefinitions_ExpectContainersToRunUsingTheDefinedImages()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new ContainerTestEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            gatewayMock.Verify(x => x.ContainerRunAsync("alpine:3.15", string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.ContainerRunAsync("redis:alpine", string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenCalled_ExpectAllContainersToBeRemoved()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            gatewayMock.Setup(x => x.ContainerRunAsync("alpine:3.15", string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>())).ReturnsAsync("runningid");
            gatewayMock.Setup(x => x.ContainerRunAsync("redis:alpine", string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>())).ReturnsAsync("disposingid");

            var fixture = new ContainerTestEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            await fixture.InitializeAsync();

            // Act
            await fixture.DisposeAsync();

            // Assert
            Assert.NotNull(fixture.AlpineContainer);
            Assert.NotNull(fixture.RedisContainer);

            if (fixture.AlpineContainer is null || fixture.RedisContainer is null)
            {
                return;
            }

            gatewayMock.Verify(x => x.ContainerRemoveAsync(fixture.AlpineContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.ContainerRemoveAsync(fixture.RedisContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class SftpContainerTests : IAsyncDisposable
    {
        private class SftpContainerTestEnvironmentFixture : DockerEnvironmentFixture
        {
            public SftpContainer? GuestContainer { get; set; }

            [SftpUserAccount("testuser1", "testpassword1")]
            [SftpUserAccount(Username = "testuser2", Password = "testpassword2")]
            public SftpContainer? AccountsContainer { get; set; }

            public SftpContainerTestEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        private readonly List<DockerEnvironmentFixture> _fixtures = new List<DockerEnvironmentFixture>();

        public async ValueTask DisposeAsync()
        {
            foreach (var fixture in _fixtures)
            {
                await fixture.DisposeAsync();
            }
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithSftpContainerDefinitions_ExpectContainersToRunUsingTheSftpImage()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new SftpContainerTestEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            gatewayMock.Verify(x => x.ContainerRunAsync("atmoz/sftp:alpine", It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DisposeAsync_WhenCalled_ExpectAllContainersToBeRemoved()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            gatewayMock.Setup(x => x.ContainerRunAsync("atmoz/sftp:alpine", "guest:guest", It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>())).ReturnsAsync("guest");
            gatewayMock.Setup(x => x.ContainerRunAsync("atmoz/sftp:alpine", "testuser1:testpassword1 testuser2:testpassword2", It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>())).ReturnsAsync("account");

            var fixture = new SftpContainerTestEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            await fixture.InitializeAsync();

            // Act
            await fixture.DisposeAsync();

            // Assert
            Assert.NotNull(fixture.GuestContainer);
            Assert.NotNull(fixture.AccountsContainer);

            if (fixture.GuestContainer is null || fixture.AccountsContainer is null)
            {
                return;
            }

            gatewayMock.Verify(x => x.ContainerRemoveAsync(fixture.GuestContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.ContainerRemoveAsync(fixture.AccountsContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class NetworkTests : IAsyncDisposable
    {
        [Network("network1")]
        [Network("network2")]
        private class NetworkTestEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            [NetworkAlias("network1", "alpine.example.com")]
            public Container? AlpineContainer { get; set; }

            [NetworkAlias("network1", "sftp.example.com")]
            [NetworkAlias("network2", "sftp-other.example.com")]
            public SftpContainer? SftpContainer { get; set; }

            public NetworkTestEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        [Network("network1")]
        [Network("network1")]
        private class DuplicateNetworkTestEnvironmentFixture : DockerEnvironmentFixture
        {
            [Image("alpine:3.15")]
            public Container? GuestContainer { get; set; }

            public SftpContainer? AccountsContainer { get; set; }

            public DuplicateNetworkTestEnvironmentFixture(IDockerGateway dockerGateway)
                : base(dockerGateway)
            {
            }
        }

        private readonly List<DockerEnvironmentFixture> _fixtures = new List<DockerEnvironmentFixture>();

        public async ValueTask DisposeAsync()
        {
            foreach (var fixture in _fixtures)
            {
                await fixture.DisposeAsync();
            }
        }

        [Fact]
        public async Task InitializeAsync_WhenNetworksAreDefinedForAFixture_ExpectTheNetworksToBeCreated()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new NetworkTestEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            gatewayMock.Verify(x => x.NetworkCreateAsync($"network1-{fixture.InstanceId}", It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.NetworkCreateAsync($"network2-{fixture.InstanceId}", It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Ctor_WhenDuplicateNetworksAreDefinedForAFixture_ExpectAnErrorToBeThrown()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            // Act
            // Assert
            Assert.Throws<NotSupportedException>(() => new DuplicateNetworkTestEnvironmentFixture(gatewayMock.Object));
        }

        [Fact]
        public async Task InitializeAsync_WhenDuplicateNetworksAreCreatedForDifferentFixtures_ExpectTheNetworksToBeCreatedAndScopedToTheirFixture()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture1 = new NetworkTestEnvironmentFixture(gatewayMock.Object);
            var fixture2 = new NetworkTestEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture1);
            _fixtures.Add(fixture2);

            // Act
            await fixture1.InitializeAsync();
            await fixture2.InitializeAsync();

            // Assert
            gatewayMock.Verify(x => x.NetworkCreateAsync($"network1-{fixture1.InstanceId}", It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.NetworkCreateAsync($"network2-{fixture1.InstanceId}", It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.NetworkCreateAsync($"network1-{fixture2.InstanceId}", It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.NetworkCreateAsync($"network2-{fixture2.InstanceId}", It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenContainersHaveDefinedNetworkAliases_ExpectTheContainersToBeConnectedToTheDefinedNetworks()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new NetworkTestEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            Assert.NotNull(fixture.AlpineContainer);
            Assert.NotNull(fixture.SftpContainer);

            if (fixture.AlpineContainer is null || fixture.SftpContainer is null)
            {
                return;
            }

            gatewayMock.Verify(x => x.NetworkConnectAsync($"network1-{fixture.InstanceId}", fixture.AlpineContainer.Id, "alpine.example.com", It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.NetworkConnectAsync($"network1-{fixture.InstanceId}", fixture.SftpContainer.Id, "sftp.example.com", It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.NetworkConnectAsync($"network2-{fixture.InstanceId}", fixture.SftpContainer.Id, "sftp-other.example.com", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenCalled_ExpectNetworksToBeRemoved()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var fixture = new NetworkTestEnvironmentFixture(gatewayMock.Object);
            _fixtures.Add(fixture);

            await fixture.InitializeAsync();

            // Act
            await fixture.DisposeAsync();

            // Assert
            gatewayMock.Verify(x => x.NetworkRemoveAsync($"network1-{fixture.InstanceId}", It.IsAny<CancellationToken>()), Times.Once);
            gatewayMock.Verify(x => x.NetworkRemoveAsync($"network2-{fixture.InstanceId}", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

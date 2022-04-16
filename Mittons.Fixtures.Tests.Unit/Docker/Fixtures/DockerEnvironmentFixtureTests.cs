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
using Mittons.Fixtures.Models;
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

            public BuildEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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

            public ReleaseEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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

            public UnsetEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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

            public MissingEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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

            public StaticEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new BuildEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={_buildId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={_buildId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={_buildId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={_buildId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithRunDetailsFromReleaseId_ExpectTheRunIdToBeSet()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new ReleaseEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={_releaseId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={_releaseId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={_releaseId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={_releaseId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithRunDetailsFromUnsetEnvironmentVariables_ExpectTheRunIdToBeDefault()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new UnsetEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={RunAttribute.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={RunAttribute.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={RunAttribute.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={RunAttribute.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithoutRunDetails_ExpectTheRunIdToBeDefault()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new MissingEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            // Act;
            await fixture.InitializeAsync();

            // Assert
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={RunAttribute.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={RunAttribute.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={RunAttribute.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id={RunAttribute.DefaultId}")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
        }

        [Fact]
        public async Task InitializeAsync_WhenInitializedWithRunDetailsFromAStaticString_ExpectTheRunIdToBeSet()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new StaticEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network1-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id=test")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            networkGatewayMock.Verify(
                    x => x.CreateAsync(
                        $"network2-{fixture.InstanceId}",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id=test")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "alpine:3.15",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id=test")),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once
                );
            containerGatewayMock.Verify(
                    x => x.RunAsync(
                        "redis:alpine",
                        "",
                        It.Is<IEnumerable<Option>>(y => y.Any(z => z.Name == "--label" && z.Value == $"mittons.fixtures.run.id=test")),
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

            public KeepUpWithIdEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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

            public KeepUpWithoutIdEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new KeepUpWithIdEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            await fixture.InitializeAsync();

            // Act
            await fixture.DisposeAsync();

            // Assert
            networkGatewayMock.Verify(
                    x => x.RemoveAsync(
                        $"network1-{fixture.InstanceId}",
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );
            networkGatewayMock.Verify(
                    x => x.RemoveAsync(
                        $"network2-{fixture.InstanceId}",
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );

            var alpineId = fixture.AlpineContainer?.Id ?? string.Empty;
            containerGatewayMock.Verify(
                    x => x.RemoveAsync(
                        alpineId,
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );

            var redisId = fixture.RedisContainer?.Id ?? string.Empty;
            containerGatewayMock.Verify(
                    x => x.RemoveAsync(
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
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new KeepUpWithoutIdEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            await fixture.InitializeAsync();

            // Act
            await fixture.DisposeAsync();

            // Assert
            networkGatewayMock.Verify(
                    x => x.RemoveAsync(
                        $"network1-{fixture.InstanceId}",
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );
            networkGatewayMock.Verify(
                    x => x.RemoveAsync(
                        $"network2-{fixture.InstanceId}",
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );

            var alpineId = fixture.AlpineContainer?.Id ?? string.Empty;
            containerGatewayMock.Verify(
                    x => x.RemoveAsync(
                        alpineId,
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never
                );

            var redisId = fixture.RedisContainer?.Id ?? string.Empty;
            containerGatewayMock.Verify(
                    x => x.RemoveAsync(
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

            public ContainerTestEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new ContainerTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            containerGatewayMock.Verify(x => x.RunAsync("alpine:3.15", string.Empty, It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
            containerGatewayMock.Verify(x => x.RunAsync("redis:alpine", string.Empty, It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenCalled_ExpectAllContainersToBeRemoved()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            containerGatewayMock.Setup(x => x.RunAsync("alpine:3.15", string.Empty, It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>())).ReturnsAsync("runningid");
            containerGatewayMock.Setup(x => x.RunAsync("redis:alpine", string.Empty, It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>())).ReturnsAsync("disposingid");

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new ContainerTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
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

            containerGatewayMock.Verify(x => x.RemoveAsync(fixture.AlpineContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
            containerGatewayMock.Verify(x => x.RemoveAsync(fixture.RedisContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
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

            public SftpContainerTestEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new SftpContainerTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            containerGatewayMock.Verify(x => x.RunAsync("atmoz/sftp:alpine", It.IsAny<string>(), It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DisposeAsync_WhenCalled_ExpectAllContainersToBeRemoved()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            containerGatewayMock.Setup(x => x.RunAsync("atmoz/sftp:alpine", "guest:guest", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>())).ReturnsAsync("guest");
            containerGatewayMock.Setup(x => x.RunAsync("atmoz/sftp:alpine", "testuser1:testpassword1 testuser2:testpassword2", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>())).ReturnsAsync("account");

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new SftpContainerTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
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

            containerGatewayMock.Verify(x => x.RemoveAsync(fixture.GuestContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
            containerGatewayMock.Verify(x => x.RemoveAsync(fixture.AccountsContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
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

            public NetworkTestEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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

            public DuplicateNetworkTestEnvironmentFixture(IContainerGateway containerGateway, INetworkGateway networkGateway)
                : base(containerGateway, networkGateway)
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
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            // Act
            await fixture.InitializeAsync();

            // Assert
            networkGatewayMock.Verify(x => x.CreateAsync($"network1-{fixture.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
            networkGatewayMock.Verify(x => x.CreateAsync($"network2-{fixture.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Ctor_WhenDuplicateNetworksAreDefinedForAFixture_ExpectAnErrorToBeThrown()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            // Act
            // Assert
            Assert.Throws<NotSupportedException>(() => new DuplicateNetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object));
        }

        [Fact]
        public async Task InitializeAsync_WhenDuplicateNetworksAreCreatedForDifferentFixtures_ExpectTheNetworksToBeCreatedAndScopedToTheirFixture()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture1 = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            var fixture2 = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture1);
            _fixtures.Add(fixture2);

            // Act
            await fixture1.InitializeAsync();
            await fixture2.InitializeAsync();

            // Assert
            networkGatewayMock.Verify(x => x.CreateAsync($"network1-{fixture1.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
            networkGatewayMock.Verify(x => x.CreateAsync($"network2-{fixture1.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
            networkGatewayMock.Verify(x => x.CreateAsync($"network1-{fixture2.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
            networkGatewayMock.Verify(x => x.CreateAsync($"network2-{fixture2.InstanceId}", It.IsAny<IEnumerable<Option>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenContainersHaveDefinedNetworkAliases_ExpectTheContainersToBeConnectedToTheDefinedNetworks()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
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

            networkGatewayMock.Verify(x => x.ConnectAsync($"network1-{fixture.InstanceId}", fixture.AlpineContainer.Id, "alpine.example.com", It.IsAny<CancellationToken>()), Times.Once);
            networkGatewayMock.Verify(x => x.ConnectAsync($"network1-{fixture.InstanceId}", fixture.SftpContainer.Id, "sftp.example.com", It.IsAny<CancellationToken>()), Times.Once);
            networkGatewayMock.Verify(x => x.ConnectAsync($"network2-{fixture.InstanceId}", fixture.SftpContainer.Id, "sftp-other.example.com", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenCalled_ExpectNetworksToBeRemoved()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var fixture = new NetworkTestEnvironmentFixture(containerGatewayMock.Object, networkGatewayMock.Object);
            _fixtures.Add(fixture);

            await fixture.InitializeAsync();

            // Act
            await fixture.DisposeAsync();

            // Assert
            networkGatewayMock.Verify(x => x.RemoveAsync($"network1-{fixture.InstanceId}", It.IsAny<CancellationToken>()), Times.Once);
            networkGatewayMock.Verify(x => x.RemoveAsync($"network2-{fixture.InstanceId}", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

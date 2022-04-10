using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Gateways;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers;

public class HttpContainerTests
{
    public class ConstructorTests : BaseContainerTests
    {
        [Fact]
        public async Task Ctor_WhenCreated_ExpectAnHttpClientThatDoesNotDisposeTheHandler()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var container = new HttpContainer(gatewayMock.Object, Guid.NewGuid(), new Attribute[] { new Image("mendhak/http-https-echo") });
            _containers.Add(container);

            // Act
            await container.InitializeAsync();
            container.UnsecureHttpClient.Dispose();

            // Assert
            container.SecureHttpClient.Timeout = TimeSpan.FromMilliseconds(1);
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(() => container.SecureHttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost")));

            Assert.True(exception.GetType() != typeof(ObjectDisposedException));
        }

        [Fact]
        public async Task Ctor_WhenCreated_ExpectASecureHttpClientThatDoesNotDisposeTheHandler()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Any);
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            // Act
            var container = new HttpContainer(gatewayMock.Object, Guid.NewGuid(), new Attribute[] { new Image("mendhak/http-https-echo") });
            _containers.Add(container);

            container.UnsecureHttpClient.Dispose();

            // Assert
            container.SecureHttpClient.Timeout = TimeSpan.FromMilliseconds(1);
            var exception = await Assert.ThrowsAsync<Exception>(() => container.SecureHttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost")));

            Assert.True(exception.GetType() != typeof(ObjectDisposedException));
        }
    }

    public class PortBindingTests : BaseContainerTests
    {
        [Theory]
        [InlineData(8080, 49621, "192.168.0.1")]
        [InlineData(8081, 49622, "192.168.0.2")]
        public async Task InitializeAsync_WhenTheUnsecurePortIsBound_ExpectHttpClientToHaveBaseAddressSetToBinding(int containerPort, int hostPort, string containerIpAddress)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Parse(containerIpAddress));
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);
            gatewayMock.Setup(x => x.ContainerGetHostPortMappingAsync(It.IsAny<string>(), "tcp", containerPort, CancellationToken.None)).ReturnsAsync(hostPort);

            var expectedHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : containerIpAddress;
            var expectedPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? hostPort : containerPort;

            var container = new HttpContainer(gatewayMock.Object, Guid.NewGuid(), new Attribute[] { new Image("mendhak/http-https-echo"), new PortBinding { Scheme = "http", Port = containerPort } });
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

            // Assert
            Assert.Equal(new Uri($"http://{expectedHost}:{expectedPort}"), container.UnsecureHttpClient.BaseAddress);
        }

        [Theory]
        [InlineData(8080, 49621, "192.168.0.1")]
        [InlineData(8081, 49622, "192.168.0.2")]
        public async Task InitializeAsync_WhenTheSecurePortIsBound_ExpectHttpClientToHaveBaseAddressSetToBinding(int containerPort, int hostPort, string containerIpAddress)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Parse(containerIpAddress));
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);
            gatewayMock.Setup(x => x.ContainerGetHostPortMappingAsync(It.IsAny<string>(), "tcp", containerPort, CancellationToken.None)).ReturnsAsync(hostPort);

            var expectedHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : containerIpAddress;
            var expectedPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? hostPort : containerPort;

            var container = new HttpContainer(gatewayMock.Object, Guid.NewGuid(), new Attribute[] { new Image("mendhak/http-https-echo"), new PortBinding { Scheme = "https", Port = containerPort } });
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

            // Assert
            Assert.Equal(new Uri($"https://{expectedHost}:{expectedPort}"), container.SecureHttpClient.BaseAddress);
        }

        [Theory]
        [InlineData(49621, "192.168.0.1")]
        [InlineData(49622, "192.168.0.2")]
        public async Task InitializeAsync_WhenTheUnsecurePortIsNotBound_ExpectHttpClientToHaveBaseAddressSetToTheDefault(int hostPort, string containerIpAddress)
        {
            // Arrange
            var containerPort = 80;

            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Parse(containerIpAddress));
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);
            gatewayMock.Setup(x => x.ContainerGetHostPortMappingAsync(It.IsAny<string>(), "tcp", containerPort, CancellationToken.None)).ReturnsAsync(hostPort);

            var expectedHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : containerIpAddress;
            var expectedPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? hostPort : containerPort;

            var container = new HttpContainer(gatewayMock.Object, Guid.NewGuid(), new Attribute[] { new Image("mendhak/http-https-echo") });
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

            // Assert
            Assert.Equal(new Uri($"http://{expectedHost}:{expectedPort}"), container.UnsecureHttpClient.BaseAddress);
        }

        [Theory]
        [InlineData(49621, "192.168.0.1")]
        [InlineData(49622, "192.168.0.2")]
        public async Task InitializeAsync_WhenTheSecurePortIsNotBound_ExpectHttpClientToHaveBaseAddressSetToTheDefault(int hostPort, string containerIpAddress)
        {
            // Arrange
            var containerPort = 443;

            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.ContainerGetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Parse(containerIpAddress));
            gatewayMock.Setup(x => x.ContainerGetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);
            gatewayMock.Setup(x => x.ContainerGetHostPortMappingAsync(It.IsAny<string>(), "tcp", containerPort, CancellationToken.None)).ReturnsAsync(hostPort);

            var expectedHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : containerIpAddress;
            var expectedPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? hostPort : containerPort;

            var container = new HttpContainer(gatewayMock.Object, Guid.NewGuid(), new Attribute[] { new Image("mendhak/http-https-echo") });
            _containers.Add(container);

            // Act
            await container.InitializeAsync();

            // Assert
            Assert.Equal(new Uri($"https://{expectedHost}:{expectedPort}"), container.SecureHttpClient.BaseAddress);
        }
    }
}
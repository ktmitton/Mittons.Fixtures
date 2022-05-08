using System;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Containers.Resources;
using Mittons.Fixtures.Core.Resources;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit;

public class FileResourceAdapterTests
{
    [Theory]
    [InlineData("123456", "/path/to/file.txt", "567879")]
    [InlineData("4567", "/other.csv", "1234")]
    public async Task SetPermissionsAsync_WhenCalled_ExpectPermissionsToBeUpdated(string expectedContainerId, string expectedPath, string expectedPermissions)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.SetPermissionsAsync(expectedPermissions, cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.SetFileSystemResourcePermissionsAsync(expectedContainerId, expectedPath, expectedPermissions, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SetPermissionsAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/file.txt"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.SetPermissionsAsync("777", cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.SetFileSystemResourcePermissionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Theory]
    [InlineData("123456", "/path/to/file.txt", "root")]
    [InlineData("4567", "/other.csv", "guest")]
    public async Task SetOwnerAsync_WhenCalled_ExpectOwnerToBeUpdated(string expectedContainerId, string expectedPath, string expectedOwner)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.SetOwnerAsync(expectedOwner, cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.SetFileSystemResourceOwnerAsync(expectedContainerId, expectedPath, expectedOwner, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SetOwnerAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/file.txt"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.SetOwnerAsync("root", cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.SetFileSystemResourceOwnerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Theory]
    [InlineData("123456", "/path/to/file.txt")]
    [InlineData("4567", "/other.csv")]
    public async Task CreateAsync_WhenCalled_ExpectFileToBeCreated(string expectedContainerId, string expectedPath)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.CreateAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.CreateFileAsync(expectedContainerId, expectedPath, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task CreateAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/file.txt"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.CreateAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.CreateFileAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Theory]
    [InlineData("123456", "/path/to/file.txt")]
    [InlineData("4567", "/other.csv")]
    public async Task DeleteAsync_WhenCalled_ExpectFileToBeDeleted(string expectedContainerId, string expectedPath)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.DeleteAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.DeleteFileAsync(expectedContainerId, expectedPath, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task DeleteAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/file.txt"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.DeleteAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Theory]
    [InlineData("123456", "/path/to/file.txt", "test")]
    [InlineData("4567", "/other.csv", "other")]
    public async Task AppendAsync_WhenCalled_ExpectContentsToBeAppended(string expectedContainerId, string expectedPath, string expectedContents)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.AppendAsync(expectedContents, cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.AppendFileAsync(expectedContainerId, expectedPath, expectedContents, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AppendAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/file.txt"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.AppendAsync(string.Empty, cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.AppendFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Theory]
    [InlineData("123456", "/path/to/file.txt", "test")]
    [InlineData("4567", "/other.csv", "other")]
    public async Task WriteAsync_WhenCalled_ExpectContentsToBeWritten(string expectedContainerId, string expectedPath, string expectedContents)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.WriteAsync(expectedContents, cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.WriteFileAsync(expectedContainerId, expectedPath, expectedContents, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WriteAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/file.txt"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.WriteAsync(string.Empty, cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.WriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Theory]
    [InlineData("123456", "/path/to/file.txt", "test")]
    [InlineData("4567", "/other.csv", "other")]
    public async Task ReadAsync_WhenCalled_ExpectContentsToBeReturned(string expectedContainerId, string expectedPath, string expectedContents)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        mockContainerGateway.Setup(x => x.ReadFileAsync(expectedContainerId, expectedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContents);
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        var actualContents = await adapter.ReadAsync(cancellationToken);

        // Assert
        Assert.Equal(expectedContents, actualContents);
    }

    [Fact]
    public async Task ReadAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/file.txt"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new FileResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.ReadAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Containers.Resources;
using Mittons.Fixtures.Core.Resources;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit;

public class DirectoryAdapterTests
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
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

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
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

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
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

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
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.SetOwnerAsync("root", cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.SetFileSystemResourceOwnerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Theory]
    [InlineData("123456", "/path/to/directory")]
    [InlineData("4567", "/other")]
    public async Task CreateAsync_WhenCalled_ExpectDirectoryToBeCreated(string expectedContainerId, string expectedPath)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.CreateAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.CreateDirectoryAsync(expectedContainerId, expectedPath, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task CreateAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/directory"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.CreateAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.CreateDirectoryAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Theory]
    [InlineData("123456", "/path/to/directory", true)]
    [InlineData("4567", "/other", false)]
    public async Task DeleteAsync_WhenCalled_ExpectDirectoryToBeDeleted(string expectedContainerId, string expectedPath, bool recursive)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.DeleteAsync(recursive, cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.DeleteDirectoryAsync(expectedContainerId, expectedPath, recursive, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task DeleteAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/directory"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.DeleteAsync(false, cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.DeleteDirectoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), cancellationToken));
    }

    [Theory]
    [InlineData("123456", "/path/to/directory")]
    [InlineData("4567", "/other")]
    public async Task EnumerateDirectoriesAsync_WhenCalled_ExpectDirectoriesToBeEnumerated(string expectedContainerId, string expectedPath)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.EnumerateDirectoriesAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.EnumerateDirectoriesAsync(expectedContainerId, expectedPath, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task EnumerateDirectoriesAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/directory"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.EnumerateDirectoriesAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.EnumerateDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Fact]
    public async Task EnumerateDirectoriesAsync_WhenCalled_ExpectSubdirectoriesToBeReturned()
    {
        // Arrange
        var expectedSubdirectories = new List<DirectoryResourceAdapter>
        {
            new DirectoryResourceAdapter("1234", "test path", default),
            new DirectoryResourceAdapter("other", "other path", default),
        };
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/directory"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        mockContainerGateway.Setup(x => x.EnumerateDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSubdirectories);
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        var actualSubDirectories = await adapter.EnumerateDirectoriesAsync(cancellationToken);

        // Assert
        Assert.Equal(expectedSubdirectories, actualSubDirectories);
    }

    [Theory]
    [InlineData("123456", "/path/to/file.txt")]
    [InlineData("4567", "/other")]
    public async Task EnumerateFilesAsync_WhenCalled_ExpectFilesToBeEnumerated(string expectedContainerId, string expectedPath)
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.{expectedContainerId}{expectedPath}"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.EnumerateFilesAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.EnumerateFilesAsync(expectedContainerId, expectedPath, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task EnumerateFilesAsync_WhenCalledWithACancellationToken_ExpectCancellationTokenToBePassedThrough()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/directory"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        await adapter.EnumerateFilesAsync(cancellationToken);

        // Assert
        mockContainerGateway.Verify(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken));
    }

    [Fact]
    public async Task EnumerateFilesAsync_WhenCalled_ExpectFilesToBeReturned()
    {
        // Arrange
        var expectedFiles = new List<FileResourceAdapter>
        {
            new FileResourceAdapter("1234", "test path", default),
            new FileResourceAdapter("other", "other path", default),
        };
        var cancellationToken = new CancellationTokenSource().Token;
        var resource = Mock.Of<IResource>(x => x.GuestUri == new Uri($"file://container.test/directory"));
        var mockContainerGateway = new Mock<IContainerGateway>();
        mockContainerGateway.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFiles);
        var adapter = new DirectoryResourceAdapter(resource, mockContainerGateway.Object);

        // Act
        var actualFiles = await adapter.EnumerateFilesAsync(cancellationToken);

        // Assert
        Assert.Equal(expectedFiles, actualFiles);
    }
}

using Mittons.Fixtures.FrameworkExtensions.Xunit.Docker.Fixtures;
using Moq;
using Xunit;

namespace Mittons.Fixtures.FrameworkExtensions.Xunit.Tests.Unit;

public class XunitAsyncLifetimeDockerEnvironmentFixtureTests
{
    [Fact]
    public void InitializeAsync_ShouldBeInvoked_WhenXunitIAsyncLifetimeIsImplemented()
    {

        var mockXunitAsyncLifetimeDockerEnvironmentFixture = new Mock<DockerEnvironmentFixture>();

        mockXunitAsyncLifetimeDockerEnvironmentFixture.Object.InitializeAsync();

        mockXunitAsyncLifetimeDockerEnvironmentFixture.Verify(xunitAsyncLifetimeDockerEnvironmentFixture =>
            xunitAsyncLifetimeDockerEnvironmentFixture.InitializeAsync(),
            Times.Once
        );
    }
}
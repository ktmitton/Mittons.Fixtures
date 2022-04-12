using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Fixtures;

namespace TestSite.Tests.Fixtures;

public class TestEnvironmentFixture : DockerEnvironmentFixture
{
    [Image("test2")]
    [PortBinding(Protocol = Protocol.Tcp, Scheme = "http", Port = 5232)]
    [Build("../../../../testsite.Dockerfile", "../../../../")]
    public HttpContainer TestSiteContainer { get; set; }
}

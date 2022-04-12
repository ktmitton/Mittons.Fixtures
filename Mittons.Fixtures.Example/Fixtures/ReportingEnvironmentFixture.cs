using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Fixtures;

namespace Mittons.Fixtures.Example.Fixtures;

public class ReportingEnvironmentFixture : DockerEnvironmentFixture, Xunit.IAsyncLifetime
{
    [SftpUserAccount("admin", "securepassword")]
    [SftpUserAccount("tswift", "hatersgonnahate")]
    public SftpContainer SftpContainer { get; set; }
}

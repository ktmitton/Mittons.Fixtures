using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.FrameworkExtensions.Xunit.Docker.Fixtures;

namespace Mittons.Fixtures.Example.Fixtures;

public class ReportingEnvironmentFixture : DockerEnvironmentFixture
{
    [SftpUserAccount("admin", "securepassword")]
    [SftpUserAccount("tswift", "hatersgonnahate")]
    public SftpContainer SftpContainer { get; set; }
}

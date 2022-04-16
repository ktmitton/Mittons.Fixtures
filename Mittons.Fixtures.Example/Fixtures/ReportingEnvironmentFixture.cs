using System;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Fixtures;
using Mittons.Fixtures.Extensions;

namespace Mittons.Fixtures.Example.Fixtures;

public class ReportingEnvironmentFixture : DockerEnvironmentFixture, Xunit.IAsyncLifetime
{
    [SftpUserAccount("admin", "securepassword")]
    [SftpUserAccount("tswift", "hatersgonnahate")]
    public SftpContainer SftpContainer { get; set; }

    public override Task InitializeAsync()
        => base.InitializeAsync(new CancellationToken().CreateLinkedTimeoutToken(TimeSpan.FromMinutes(5)));
}

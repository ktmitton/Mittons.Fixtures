using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mittons.Fixtures.FrameworkExtensions.Xunit.Docker.Containers;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers;

public abstract class BaseSftpContainerTests : IAsyncDisposable
{
    protected readonly List<SftpContainer> _sftpContainers = new List<SftpContainer>();

    public async ValueTask DisposeAsync()
    {
        foreach(var sftpContainer in _sftpContainers)
        {
            await sftpContainer.DisposeAsync();
        }
    }
}

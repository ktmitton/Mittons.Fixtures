using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Containers;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers
{
    public abstract class BaseContainerTests : IAsyncDisposable
    {
        protected readonly List<Container> _containers = new List<Container>();

        public async ValueTask DisposeAsync()
        {
            foreach (var container in _containers)
            {
                await container.DisposeAsync();
            }
        }
    }
}
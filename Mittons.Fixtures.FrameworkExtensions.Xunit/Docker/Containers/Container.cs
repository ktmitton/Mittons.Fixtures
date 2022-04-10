using System;
using System.Collections.Generic;
using Mittons.Fixtures.Docker.Gateways;
using XunitIAsyncLifetime = Xunit.IAsyncLifetime;

namespace Mittons.Fixtures.FrameworkExtensions.Xunit.Docker.Containers
{
    /// <summary>
    /// Adapt <see cref="IAsyncLifetime"/> to <see cref="XunitIAsyncLifetime"/> functionality 
    /// for <see cref="Mittons.Fixtures.Docker.Containers.Container"/>.
    /// </summary>
    public class Container : Mittons.Fixtures.Docker.Containers.Container, XunitIAsyncLifetime
    {
        public Container(
            IDockerGateway dockerGateway,
            Guid instanceId,
            IEnumerable<Attribute> attributes
        )
            : base(
                dockerGateway,
                instanceId,
                attributes
            )
        {

        }
    }
}

using System.Collections.Generic;
using Mittons.Fixtures.Docker.Gateways;
using XunitIAsyncLifetime = Xunit.IAsyncLifetime;

namespace Mittons.Fixtures.FrameworkExtensions.Xunit.Docker.Fixtures
{
    /// <summary>
    /// Adapt <see cref="IAsyncLifetime"/> to <see cref="XunitIAsyncLifetime"/> functionality 
    /// for <see cref="Mittons.Fixtures.Docker.Networks.DefaultNetwork"/>.
    /// </summary>
    public class DefaultNetwork : Mittons.Fixtures.Docker.Networks.DefaultNetwork, XunitIAsyncLifetime
    {
        public DefaultNetwork(
            IDockerGateway dockerGateway,
            string name,
            IEnumerable<KeyValuePair<string, string>> options
        )
            : base(
                dockerGateway,
                name,
                options
            )
        {

        }
    }
}
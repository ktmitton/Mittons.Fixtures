using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Gateways;
using XunitIAsyncLifetime = Xunit.IAsyncLifetime;

namespace Mittons.Fixtures.FrameworkExtensions.Xunit.Docker.Fixtures
{
    /// <summary>
    /// Adapt <see cref="IAsyncLifetime"/> to <see cref="XunitIAsyncLifetime"/> functionality 
    /// for <see cref="Mittons.Fixtures.Docker.Fixtures.DockerEnvironmentFixture"/>.
    /// </summary>
    public abstract class DockerEnvironmentFixture : Mittons.Fixtures.Docker.Fixtures.DockerEnvironmentFixture, XunitIAsyncLifetime
    {
        public DockerEnvironmentFixture()
            : base()
        {

        }

        public DockerEnvironmentFixture(IDockerGateway dockerGateway)
            : base(dockerGateway)
        {

        }

        public async Task InitializeAsync()
        {
            await InitializeAsync(CancellationToken.None);
        }
    }
}

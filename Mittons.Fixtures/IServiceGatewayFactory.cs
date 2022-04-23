using System;

namespace Mittons.Fixtures
{
    /// <summary>
    /// A factory for retrieving/creating the <see cref="Mittons.Fixtures.IServiceGateway{TService}"/> needed to create an <see cref="Mittons.Fixtures.IService" />.
    /// </summary>
    /// <remarks>
    /// By default, the <see cref="Mittons.Fixtures.GuestEnvironmentFixture"/> will create a factory that registers all gateways provided by the library, but this can be overriden with custom factories if needed, such as in unit tests.
    /// </remarks>
    public interface IServiceGatewayFactory
    {
        /// <summary>
        /// Given a <see cref="System.Type"/> that implements <see cref="Mittons.Fixtures.Resources.IService"/>, this will find a registered <see cref="Mittons.Fixtures.IServiceGateway{TService}"/> capable of creating a new instance of the <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </summary>
        /// <param name="serviceType">
        /// The <see cref="System.Type"/> of <see cref="Mittons.Fixtures.Resources.IService"/> the underlying <see cref="Mittons.Fixtures.IServiceGateway{TService}"/> should be capable of creating.
        /// </param>
        /// <remarks>
        /// The returned <see cref="Mittons.Fixtures.IServiceGateway{IService}"/> is typically of decorator of an <see cref="Mittons.Fixtures.IServiceGateway{TService}"/> to create a generic interface used by <see cref="Mittons.Fixtures.GuestEnvironmentFixture"/> instances to create instances of <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </remarks>
        /// <returns>
        /// An instance of an <see cref="Mittons.Fixtures.IServiceGateway{IService}"/> that can be used to create new instances of <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </returns>
        /// <value>
        /// A generic <see cref="Mittons.Fixtures.IServiceGateway{IService}"/> that hides the underlying details of the actual <see cref="Mittons.Fixtures.IServiceGateway{TService}"/> creating instance of <see cref="Mittons.Fixtures.Resources.IService"/>.
        /// </value>
        IServiceGateway<IService> GetServiceGateway(Type serviceType);
    }
}

using System;

namespace Mittons.Fixtures
{
    /// <summary>
    /// Represents details needed for the Host environment to communicate with the Guest <see cref="Mittons.Fixtures.Resources.IService"/>.
    /// </summary>
    /// <remarks>
    /// These communications details can be a variety of mechanisms, such as file changes or network messaging.
    /// </remarks>
    public interface IResource
    {
        /// <summary>
        /// Gets the details for how the <see cref="Mittons.Fixtures.IService"/> monitors a resource to trigger actions.
        /// </summary>
        /// <remarks>
        /// This can represent asynchronous communication mechanisms such as monitoring a file for changes or more synchronous request-respones channels such as http connections.
        /// </remarks>
        /// <returns>
        /// Details describing the resource being monitored by the <see cref="Mittons.Fixtures.IResource"/>.
        /// </returns>
        /// <value>
        /// A <see href="https://en.wikipedia.org/wiki/Uniform_Resource_Identifier">Uniform Resource Identifier</see> with all known details on how the <see cref="Mittons.Fixtures.IResource"/> listens for action triggers.
        /// </value>
        Uri GuestUri { get; }

        /// <summary>
        /// Gets the details for how the Host can communicate with an <see cref="Mittons.Fixtures.IService"/>.
        /// </summary>
        /// <remarks>
        /// This can represent asynchronous communication mechanisms such as monitoring a file for changes or more synchronous request-respones channels such as http connections.
        /// </remarks>
        /// <returns>
        /// Details describing the resource that can be accessed by the Host to communicate with the <see cref="Mittons.Fixtures.IService"/>.
        /// </returns>
        /// <value>
        /// A <see href="https://en.wikipedia.org/wiki/Uniform_Resource_Identifier">Uniform Resource Identifier</see> with all known details on how the Host communicates with the <see cref="Mittons.Fixtures.IService"/>.
        /// </value>
        Uri HostUri { get; }
    }
}

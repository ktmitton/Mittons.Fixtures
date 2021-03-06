using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Core.Services
{
    /// <summary>
    /// Represents details describing the capabilities of an <see cref="Mittons.Fixtures.IService"/>.
    /// </summary>
    /// <remarks>
    /// The details typically include methods through which the Host environment can communicate with the Guest <see cref="Mittons.Fixtures.IService"/>.
    /// </remarks>
    public interface IService : IAsyncDisposable
    {
        string ServiceId { get; }

        /// <summary>
        /// Gets the <see cref="Mittons.Fixtures.IResource">IResources</see> exposed by the <see cref="Mittons.Fixtures.IService"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="Mittons.Fixtures.IResource"/> can be a variety of resources, such as a file or tcp resource.
        /// </remarks>
        /// <returns>
        /// A collection of all <see cref="Mittons.Fixtures.IResource">IResources</see> exposed by the <see cref="Mittons.Fixtures.IService"/>.
        /// </returns>
        /// <value>
        /// A <see cref="Mittons.Fixtures.IResource"/> is typically used by the Host to communicate with the Guest <see cref="Mittons.Fixtures.IService"/>.
        /// </value>
        IEnumerable<IResource> Resources { get; }

        Task InitializeAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken);
    }
}

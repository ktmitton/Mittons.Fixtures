using System;
using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IEnumerable"/>.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Gets the collection of options as a string formatted for use as execution parameters in a <c>docker</c> command.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>The options as a string formatted for use in a <c>docker</c> command.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the options, any option, or their properties, are <c>null</c>.</exception>
        internal static string ToExecutionParametersFormattedString(this IEnumerable<Option> options)
            => string.Join(" ", (options ?? Enumerable.Empty<Option>()).Select(option => option.ExecutionParameter).ToList());
    }
}

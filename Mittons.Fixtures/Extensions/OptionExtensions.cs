using System;
using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="Option"/>.
    /// </summary>
    public static class OptionExtensions
    {
        /// <summary>
        /// Gets the option as a string formatted for use as an execution parameter in a <c>docker</c> command.
        /// </summary>
        /// <param name="option"></param>
        /// <returns>The option as a string formatted for use in a <c>docker</c> command.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the option, or its properties, are <c>null</c>.</exception>
        public static string ToExecutionParameterFormattedString(this Option option)
        {
            if(option is null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            if(option.Name is null)
            {
                throw new ArgumentNullException(nameof(option.Name));
            }

            if(option.Value is null)
            {
                throw new ArgumentNullException(nameof(option.Value));
            }

            return $"{option.Name} \"{option.Value}\"";
        }

        /// <summary>
        /// Gets the collection of options as a string formatted for use as execution parameters in a <c>docker</c> command.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>The options as a string formatted for use in a <c>docker</c> command.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the options, any option, or their properties, are <c>null</c>.</exception>
        public static string ToExecutionParametersFormattedString(this IEnumerable<Option> options)
        {
            if(options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var formattedOptions = options.Select(option => option.ToExecutionParameterFormattedString())
                .ToList();

            return formattedOptions.Any() ? string.Join(" ", formattedOptions) : string.Empty;
        }
    }
}

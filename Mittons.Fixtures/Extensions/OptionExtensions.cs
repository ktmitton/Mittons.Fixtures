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
        /// Gets the option as a string formatted for use in a docker command.
        /// </summary>
        /// <param name="option"></param>
        /// <returns>The option as a string formatted for use in a docker command.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the option, or its properties, are <c>null</c>.</exception>
        public static string ToCommandFormattedString(this Option option)
        {
            if(option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            if(option.Name == null)
            {
                throw new ArgumentNullException(nameof(option.Name));
            }

            if(option.Value == null)
            {
                throw new ArgumentNullException(nameof(option.Value));
            }

            return $"{option.Name} \"{option.Value}\"";
        }

        /// <summary>
        /// Gets the collection of options as a string formatted for use in a docker command.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>The options as a string formatted for use in a docker command.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the options, any option, or their properties, are <c>null</c>.</exception>
        public static string ToCommandFormattedString(this IEnumerable<Option> options)
        {
            if(options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var formattedOptions = options.Select(option => option.ToCommandFormattedString())
                .ToList();

            return formattedOptions.Any() ? string.Join(" ", formattedOptions) : string.Empty;
        }
    }
}

using System;

namespace Mittons.Fixtures.Core.Models
{
    /// <summary>
    /// A docker option used to further configure a command.
    /// </summary>
    public class Option
    {
        /// <summary>
        /// The name of the option.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value for the option.
        /// </summary>
        /// <remarks>
        /// Set as a direct value with no quotations.
        /// Set to <c>null</c>, <c>empty string</c>, or <c>white-space</c> if there is no value to accompany the option name.
        /// </remarks>
        public string Value { get; set; }

        /// <summary>
        /// The parameter string that can be inserted in the execution string.
        /// </summary>
        public string ExecutionParameter
        {
            get
            {
                if (Name is null)
                {
                    throw new ArgumentNullException();
                }

                return string.IsNullOrWhiteSpace(Value) ? Name : $"{Name} \"{Value}\"";
            }
        }
    }
}

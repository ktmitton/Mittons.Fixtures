namespace Mittons.Fixtures.Models
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
        public string Value { get; set; }
    }
}

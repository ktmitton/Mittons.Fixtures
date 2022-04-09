using System.Collections.Generic;

namespace Mittons.Fixtures.Docker.Attributes
{
    public interface IOptionAttribute
    {
        IEnumerable<KeyValuePair<string, string>> Options { get; }
    }
}
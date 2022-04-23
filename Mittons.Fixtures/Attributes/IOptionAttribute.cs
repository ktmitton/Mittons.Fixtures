using System.Collections.Generic;
using Mittons.Fixtures.Models;

namespace Mittons.Fixtures.Attributes
{
    public interface IOptionAttribute
    {
        IEnumerable<Option> Options { get; }
    }
}
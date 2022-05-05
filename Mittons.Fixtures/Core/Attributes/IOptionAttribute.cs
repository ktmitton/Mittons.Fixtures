using System.Collections.Generic;
using Mittons.Fixtures.Core.Models;

namespace Mittons.Fixtures.Core.Attributes
{
    public interface IOptionAttribute
    {
        IEnumerable<Option> Options { get; }
    }
}
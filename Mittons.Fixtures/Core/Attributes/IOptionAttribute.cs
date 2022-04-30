using System.Collections.Generic;
using Mittons.Fixtures.Core.Models;

namespace Mittons.Fixtures.Attributes
{
    public interface IOptionAttribute
    {
        IEnumerable<Option> Options { get; }
    }
}
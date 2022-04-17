using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Extensions;
using Mittons.Fixtures.Models;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Extensions;

public class OptionExtensionsTests
{
    [Fact]
    public void ToExecutionParametersFormattedString_WhenOptionsIsNull_ExpectEmptyString()
    {
        IEnumerable<Option>? stubOptions = null;

        var commandFormattedString = stubOptions.ToExecutionParametersFormattedString();

        Assert.Equal(string.Empty, commandFormattedString);
    }

    [Fact]
    public void ToExecutionParametersFormattedString_WhenOptionsIsEmpty_ExpectEmptyString()
    {
        var stubOptions = Enumerable.Empty<Option>();

        var commandFormattedString = stubOptions.ToExecutionParametersFormattedString();

        Assert.Equal(string.Empty, commandFormattedString);
    }

    [Fact]
    public void ToExecutionParametersFormattedString_WhenOptionsHasElement_ExpectExecutionParameterFormattedString()
    {
        var stubOptions = new Option[]
        {
            new Option
            {
                Name = "--option",
                Value = "value"
            }
        };

        var commandFormattedString = stubOptions.ToExecutionParametersFormattedString();

        Assert.Equal("--option \"value\"", commandFormattedString);
    }

    [Fact]
    public void ToExecutionParametersFormattedString_WhenOptionsHasElements_ExpectExecutionParameterFormattedString()
    {
        var stubOptions = new Option[]
        {
            new Option
            {
                Name = "--option",
                Value = "value"
            },
            new Option
            {
                Name = "--option",
                Value = "value"
            }
        };

        var commandFormattedString = stubOptions.ToExecutionParametersFormattedString();

        Assert.Equal("--option \"value\" --option \"value\"", commandFormattedString);
    }
}

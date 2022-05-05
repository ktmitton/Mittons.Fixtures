using System;
using Mittons.Fixtures.Core.Models;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Core.Models;

public class OptionTests
{
    [Fact]
    public void ExecutionParameter_WhenNameIsNull_ExpectArgumentNullException()
    {
        var stubOption = new Option
        {
            Name = null,
            Value = "value"
        };

        Assert.Throws<ArgumentNullException>(() => stubOption.ExecutionParameter);
    }

    [Fact]
    public void ExecutionParameter_WhenValueIsNull_ExpectExecutionParameterFormattedStringWithOnlyName()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = null
        };

        var commandFormattedString = stubOption.ExecutionParameter;

        Assert.Equal("--option", commandFormattedString);
    }

    [Fact]
    public void ExecutionParameter_WhenValueIsEmptyString_ExpectExecutionParameterFormattedStringWithOnlyName()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = string.Empty
        };

        var commandFormattedString = stubOption.ExecutionParameter;

        Assert.Equal("--option", commandFormattedString);
    }

    [Fact]
    public void ExecutionParameter_WhenValueIsWhiteSpace_ExpectExecutionParameterFormattedStringWithOnlyName()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = " "
        };

        var commandFormattedString = stubOption.ExecutionParameter;

        Assert.Equal("--option", commandFormattedString);
    }

    [Fact]
    public void ExecutionParameter_WhenNameAndValueAreNotNull_ExpectExecutionParameterFormattedString()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = "value"
        };

        var commandFormattedString = stubOption.ExecutionParameter;

        Assert.Equal("--option \"value\"", commandFormattedString);
    }
}

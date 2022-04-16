using System;
using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Extensions;
using Mittons.Fixtures.Models;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Extensions;

public class OptionExtensionsTests
{
    [Fact]
    public void ToExecutionParameterFormattedString_WhenOptionIsNull_ExpectArgumentNullException()
    {
        Option? stubOption = null;

        Assert.Throws<ArgumentNullException>(() => stubOption.ToExecutionParameterFormattedString());
    }

    [Fact]
    public void ToExecutionParameterFormattedString_WhenNameIsNull_ExpectArgumentNullException()
    {
        var stubOption = new Option
        {
            Name = null,
            Value = "value"
        };

        Assert.Throws<ArgumentNullException>(() => stubOption.ToExecutionParameterFormattedString());
    }

    [Fact]
    public void ToExecutionParameterFormattedString_WhenValueIsNull_ExpectExecutionParameterFormattedStringWithOnlyName()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = null
        };

        var commandFormattedString = stubOption.ToExecutionParameterFormattedString();

        Assert.Equal("--option", commandFormattedString);
    }

    [Fact]
    public void ToExecutionParameterFormattedString_WhenValueIsEmptyString_ExpectExecutionParameterFormattedStringWithOnlyName()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = string.Empty
        };

        var commandFormattedString = stubOption.ToExecutionParameterFormattedString();

        Assert.Equal("--option", commandFormattedString);
    }

    [Fact]
    public void ToExecutionParameterFormattedString_WhenValueIsWhiteSpace_ExpectExecutionParameterFormattedStringWithOnlyName()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = " "
        };

        var commandFormattedString = stubOption.ToExecutionParameterFormattedString();

        Assert.Equal("--option", commandFormattedString);
    }

    [Fact]
    public void ToExecutionParameterFormattedString_WhenNameAndValueAreNotNull_ExpectExecutionParameterFormattedString()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = "value"
        };

        var commandFormattedString = stubOption.ToExecutionParameterFormattedString();

        Assert.Equal("--option \"value\"", commandFormattedString);
    }

    [Fact]
    public void ToExecutionParametersFormattedString_WhenOptionsIsNull_ExpectArgumentNullException()
    {
        IEnumerable<Option>? stubOptions = null;

        Assert.Throws<ArgumentNullException>(() => stubOptions.ToExecutionParametersFormattedString());
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

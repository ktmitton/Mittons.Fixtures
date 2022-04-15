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
    public void ToCommandFormattedString_WhenOptionIsNull_ExpectArgumentNullException()
    {
        Option stubOption = null;

        Assert.Throws<ArgumentNullException>(() => stubOption.ToCommandFormattedString());
    }

    [Fact]
    public void ToCommandFormattedString_WhenNameIsNull_ExpectArgumentNullException()
    {
        var stubOption = new Option
        {
            Name = null,
            Value = "value"
        };

        Assert.Throws<ArgumentNullException>(() => stubOption.ToCommandFormattedString());
    }

    [Fact]
    public void ToCommandFormattedString_WhenValueIsNull_ExpectArgumentNullException()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = null
        };

        Assert.Throws<ArgumentNullException>(() => stubOption.ToCommandFormattedString());
    }

    [Fact]
    public void ToCommandFormattedString_WhenNameAndValueAreNotNull_ExpectCommandFormattedString()
    {
        var stubOption = new Option
        {
            Name = "--option",
            Value = "value"
        };

        var commandFormattedString = stubOption.ToCommandFormattedString();

        Assert.Equal("--option \"value\"", commandFormattedString);
    }

    [Fact]
    public void ToCommandFormattedString_WhenOptionsIsNull_ExpectArgumentNullException()
    {
        IEnumerable<Option> stubOptions = null;

        Assert.Throws<ArgumentNullException>(() => stubOptions.ToCommandFormattedString());
    }

    [Fact]
    public void ToCommandFormattedString_WhenOptionsIsEmpty_ExpectEmptyString()
    {
        var stubOptions = Enumerable.Empty<Option>();

        var commandFormattedString = stubOptions.ToCommandFormattedString();

        Assert.Equal(String.Empty, commandFormattedString);
    }

    [Fact]
    public void ToCommandFormattedString_WhenOptionsHasElement_ExpectCommandFormattedString()
    {
        var stubOptions = new List<Option>
        {
            new Option
            {
                Name = "--option",
                Value = "value"
            }
        };

        var commandFormattedString = stubOptions.ToCommandFormattedString();

        Assert.Equal("--option \"value\"", commandFormattedString);
    }

    [Fact]
    public void ToCommandFormattedString_WhenOptionsHasElements_ExpectCommandFormattedString()
    {
        var stubOptions = new List<Option>
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

        var commandFormattedString = stubOptions.ToCommandFormattedString();

        Assert.Equal("--option \"value\" --option \"value\"", commandFormattedString);
    }
}

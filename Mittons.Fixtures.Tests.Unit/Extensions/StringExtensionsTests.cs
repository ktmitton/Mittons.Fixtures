using System;
using System.Collections.Generic;
using Mittons.Fixtures.Extensions;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [MemberData(nameof(GetTestParameters))]
    public void ReplaceEnvironmentVariables((string Name, string Value)[] environmentVariables, string template, string expectedResult)
    {
        // Arrange
        foreach (var variable in environmentVariables)
        {
            Environment.SetEnvironmentVariable(variable.Name, variable.Value);
        }

        // Act
        var result = template.ReplaceEnvironmentVariables();

        // Assert
        Assert.Equal(expectedResult, result);
    }

    public static IEnumerable<object[]> GetTestParameters()
    {
        yield return new object[]
        {
            new (string Name, string Value)[]
            {
                ("Test1", "Value1")
            },
            "my ${Test1} test string",
            "my Value1 test string"
        };

        yield return new object[]
        {
            new (string Name, string Value)[]
            {
                ("Test2", "Value2"),
                ("Other3", "Value3"),
                ("Random4", "Value4")
            },
            "my ${Test2}-${Other3}-${Random4} ${Missing5} test string",
            "my Value2-Value3-Value4  test string"
        };
    }
}

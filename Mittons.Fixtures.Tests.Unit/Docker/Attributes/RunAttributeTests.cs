using System;
using System.Collections.Generic;
using Mittons.Fixtures.Docker.Attributes;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Attributes;

public class RunAttributeTests
{
    public class IdTests
    {
        public IdTests()
        {
            Environment.SetEnvironmentVariable("Test1", "Value1");
            Environment.SetEnvironmentVariable("Test2", "Value2");
            Environment.SetEnvironmentVariable("Other3", "Value3");
            Environment.SetEnvironmentVariable("Random4", "Value4");
        }

        [Theory]
        [MemberData(nameof(GetIdTemplates))]
        public void Ctor_WhenIdContainsAnEnvironmentVariableTemplates_ExpectTemplatesToBeReplaced(
            string templatedId, string expectedResult
        )
        {
            // Arrange

            // Act
            var run = new RunAttribute(templatedId);

            // Assert
            Assert.Equal(expectedResult, run.Id);
        }

        public static IEnumerable<object[]> GetIdTemplates()
        {
            yield return new object[]
            {
                "my ${Test1} test string",
                "my Value1 test string"
            };

            yield return new object[]
            {
                "my ${Test2}-${Other3}-${Random4} ${Missing5} test string",
                "my Value2-Value3-Value4  test string"
            };
        }
    }
}

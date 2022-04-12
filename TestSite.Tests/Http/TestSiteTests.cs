using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TestSite.Tests.Fixtures;
using Xunit;

namespace TestSite.Tests.Http;

public class TestSiteTests : IClassFixture<TestEnvironmentFixture>
{
    private readonly TestEnvironmentFixture _testEnvironment;

    public TestSiteTests(TestEnvironmentFixture testEnvironment)
    {
        _testEnvironment = testEnvironment;
    }

    [Fact]
    public async Task Test1()
    {
        var result = await _testEnvironment.TestSiteContainer.UnsecureHttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/WeatherForecast"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Test2()
    {
        var result = await _testEnvironment.TestSiteContainer.UnsecureHttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/WeatherForecast/exception"));

        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
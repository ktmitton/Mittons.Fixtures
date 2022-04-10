using System.Net.Http;
using System.Threading.Tasks;
using Mittons.Fixtures.Example.Fixtures;
using Xunit;

namespace Mittons.Fixtures.Example.Http;

public class RequestTests : IClassFixture<ReportingEnvironmentFixture>
{
    private readonly ReportingEnvironmentFixture _reportingEnvironment;

    public RequestTests(ReportingEnvironmentFixture reportingEnvironment)
    {
        _reportingEnvironment = reportingEnvironment;
    }

    [Fact]
    public async Task SendAsync_WhenSendingOverUnsecure_ExpectSuccessfulResponse()
    {
        // Arrange
        var client = _reportingEnvironment.HttpContainer.UnsecureHttpClient;

        // Act
        var responseMessage = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, string.Empty));

        // Assert
        Assert.True(responseMessage.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SendAsync_WhenSendingOverSecure_ExpectSuccessfulResponse()
    {
        // Arrange
        var client = _reportingEnvironment.HttpContainer.SecureHttpClient;

        // Act
        var responseMessage = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, string.Empty));

        // Assert
        Assert.True(responseMessage.IsSuccessStatusCode);
    }
}

using Reqnroll;
using Xunit;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class RestGetSteps(ServerContext serverContext)
{
    private string? _responseBody;

    [When(@"I send a REST GET request to ""([^""]*)""")]
    public async Task WhenISendARestGetRequestTo(string path)
    {
        var response = await serverContext.Http.GetAsync($"http://{ServerContext.HttpHost}:{ServerContext.HttpPort}{path}");
        _responseBody = await response.Content.ReadAsStringAsync();
    }

    [Then(@"the response body should be ""([^""]*)""")]
    public void ThenTheResponseBodyShouldBe(string expected)
    {
        Assert.Equal(expected, _responseBody);
    }
}

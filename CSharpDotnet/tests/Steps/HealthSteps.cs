using Reqnroll;
using Xunit;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class HealthSteps(ServerContext serverContext)
{
    private string? _responseBody;

    [When("I request the health endpoint")]
    public async Task WhenIRequestTheHealthEndpoint()
    {
        var response = await serverContext.Http.GetAsync($"http://{ServerContext.HttpHost}:{ServerContext.HttpPort}/health");
        _responseBody = await response.Content.ReadAsStringAsync();
    }

    [Then("the response should be ok")]
    public void ThenTheResponseShouldBeOk()
    {
        Assert.Equal("ok", _responseBody);
    }
}

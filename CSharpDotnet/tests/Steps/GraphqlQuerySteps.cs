using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using Xunit;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class GraphqlQuerySteps(ServerContext serverContext)
{
    private JsonDocument? _responseBody;

    [When(@"I send a GraphQL query for ""([^""]*)""")]
    public async Task WhenISendAGraphQlQueryFor(string field)
    {
        var query = $"{{ {field} }}";
        var response = await serverContext.Http.PostAsJsonAsync(
            $"http://{ServerContext.HttpHost}:{ServerContext.HttpPort}/graphql",
            new { query });

        Assert.True(response.IsSuccessStatusCode);
        _responseBody = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [Then(@"the GraphQL response data should be ""([^""]*)""")]
    public void ThenTheGraphQlResponseDataShouldBe(string expected)
    {
        var actual = _responseBody!.RootElement
            .GetProperty("data")
            .GetProperty("hello")
            .GetString();

        Assert.Equal(expected, actual);
    }
}

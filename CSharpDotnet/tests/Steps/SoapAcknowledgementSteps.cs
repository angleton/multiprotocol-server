using System.Net.Http.Headers;
using System.Text;
using Reqnroll;
using Xunit;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class SoapAcknowledgementSteps(ServerContext serverContext)
{
    private string? _contentType;
    private string? _responseBody;

    [When(@"I send a SOAP request to ""([^""]*)""")]
    public async Task WhenISendASoapRequestTo(string path)
    {
        const string soapBody = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                <soap:Body>
                    <PingRequest>
                        <Message>Hello from BDD SOAP test</Message>
                    </PingRequest>
                </soap:Body>
            </soap:Envelope>
            """;

        using var content = new StringContent(soapBody, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

        var response = await serverContext.Http.PostAsync(
            $"http://{ServerContext.HttpHost}:{ServerContext.HttpPort}{path}",
            content);

        _contentType = response.Content.Headers.ContentType?.ToString();
        _responseBody = await response.Content.ReadAsStringAsync();
    }

    [Then(@"the SOAP response content type should be ""([^""]*)""")]
    public void ThenTheSoapResponseContentTypeShouldBe(string expected)
    {
        Assert.NotNull(_contentType);
        Assert.StartsWith(expected, _contentType);
    }

    [Then(@"the SOAP response body should contain ""([^""]*)""")]
    public void ThenTheSoapResponseBodyShouldContain(string expected)
    {
        Assert.NotNull(_responseBody);
        Assert.Contains(expected, _responseBody);
    }
}

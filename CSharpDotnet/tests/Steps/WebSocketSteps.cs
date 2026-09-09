using System.Net.WebSockets;
using System.Text;
using Reqnroll;
using Xunit;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class WebSocketSteps : IDisposable
{
    private ClientWebSocket? _webSocket;
    private string? _response;

    [When("I connect to the WebSocket endpoint")]
    public async Task WhenIConnectToTheWebSocketEndpoint()
    {
        _webSocket = new ClientWebSocket();
        await _webSocket.ConnectAsync(
            new Uri($"ws://{ServerContext.HttpHost}:{ServerContext.HttpPort}/ws"),
            CancellationToken.None);
    }

    [When(@"I send the WebSocket message ""([^""]*)""")]
    public async Task WhenISendTheWebSocketMessage(string message)
    {
        var webSocket = _webSocket ?? throw new InvalidOperationException("WebSocket connection was not established");

        var requestBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(requestBytes, WebSocketMessageType.Text, true, CancellationToken.None);

        var buffer = new byte[8192];
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
        _response = Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    [Then(@"the WebSocket response should be ""([^""]*)""")]
    public void ThenTheWebSocketResponseShouldBe(string expected)
    {
        Assert.Equal(expected, _response);
    }

    public void Dispose()
    {
        _webSocket?.Dispose();
    }
}

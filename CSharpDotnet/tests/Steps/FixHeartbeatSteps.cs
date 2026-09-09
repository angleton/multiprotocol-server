using System.Net.Sockets;
using System.Text;
using Reqnroll;
using Xunit;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class FixHeartbeatSteps
{
    private string? _response;

    [When("I send a FIX heartbeat message")]
    public async Task WhenISendAFixHeartbeatMessage()
    {
        using var client = new TcpClient();
        await client.ConnectAsync(ServerContext.HttpHost, ServerContext.HttpPort + 2);
        await using var stream = client.GetStream();

        var heartbeat = "8=FIX.4.4\u00019=5\u000135=0\u000110=000\u0001\n"u8.ToArray();
        await stream.WriteAsync(heartbeat);
        client.Client.Shutdown(SocketShutdown.Send);

        using var responseStream = new MemoryStream();
        await stream.CopyToAsync(responseStream);
        _response = Encoding.UTF8.GetString(responseStream.ToArray());
    }

    [Then(@"the FIX response should contain message type ""([^""]*)""")]
    public void ThenTheFixResponseShouldContainMessageType(string messageType)
    {
        Assert.NotNull(_response);
        Assert.Contains($"35={messageType}", _response);
    }
}

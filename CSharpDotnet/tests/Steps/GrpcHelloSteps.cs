using Grpc.Net.Client;
using MultiprotocolServer.Protos;
using Reqnroll;
using Xunit;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class GrpcHelloSteps
{
    private string? _responseMessage;

    [When(@"I call the gRPC hello method with the name ""([^""]*)""")]
    public async Task WhenICallTheGrpcHelloMethodWithTheName(string name)
    {
        using var channel = GrpcChannel.ForAddress(
            $"http://{ServerContext.HttpHost}:{ServerContext.HttpPort + 1}");
        var client = new Hello.HelloClient(channel);

        var response = await client.SayHelloAsync(new HelloRequest { Name = name, Payload = string.Empty });
        _responseMessage = response.Message;
    }

    [Then(@"the gRPC response message should be ""([^""]*)""")]
    public void ThenTheGrpcResponseMessageShouldBe(string expected)
    {
        Assert.Equal(expected, _responseMessage);
    }
}

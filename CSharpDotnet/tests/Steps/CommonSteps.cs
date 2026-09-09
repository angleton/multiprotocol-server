using Reqnroll;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class CommonSteps(ServerContext serverContext)
{
    [Given("the server is running")]
    public async Task GivenTheServerIsRunning()
    {
        await serverContext.StartAsync();
    }
}

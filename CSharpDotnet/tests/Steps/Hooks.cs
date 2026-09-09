using Reqnroll;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class Hooks(ServerContext serverContext)
{
    [AfterScenario]
    public async Task StopServerAsync()
    {
        await serverContext.DisposeAsync();
    }
}

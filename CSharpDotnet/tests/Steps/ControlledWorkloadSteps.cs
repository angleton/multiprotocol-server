using System.Text.Json;
using Reqnroll;
using Xunit;

namespace MultiprotocolServer.Tests.Steps;

[Binding]
public sealed class ControlledWorkloadSteps(ServerContext serverContext)
{
    private static readonly string[] Protocols = ["fix", "grpc", "graphql", "rest", "soap", "websocket"];

    private byte _targetCpuPercent;
    private ulong _targetDurationMs;
    private bool _responseSucceeded;

    [Given(@"I request a target CPU load of (\d+) percent for (\d+) milliseconds")]
    public void GivenIRequestATargetCpuLoad(byte targetCpuPercent, ulong targetDurationMs)
    {
        _targetCpuPercent = targetCpuPercent;
        _targetDurationMs = targetDurationMs;
    }

    [When("I send the controlled workload through every protocol")]
    public async Task WhenISendTheControlledWorkloadThroughEveryProtocol()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"http://{ServerContext.HttpHost}:{ServerContext.HttpPort}/hello");
        request.Headers.Add("x-workload-cpu-percent", _targetCpuPercent.ToString());
        request.Headers.Add("x-workload-duration-ms", _targetDurationMs.ToString());

        var response = await serverContext.Http.SendAsync(request);
        _responseSucceeded = response.IsSuccessStatusCode;
    }

    [Then("every protocol response should succeed")]
    public void ThenEveryProtocolResponseShouldSucceed()
    {
        Assert.True(_responseSucceeded);
    }

    [Then(@"telemetry should report the requested (\d+) percent load for (\d+) milliseconds")]
    public async Task ThenTelemetryShouldReportTheRequestedLoad(byte targetCpuPercent, ulong targetDurationMs)
    {
        var response = await serverContext.Http.GetAsync(
            $"http://{ServerContext.HttpHost}:{ServerContext.HttpPort}/telemetry");
        using var telemetry = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var protocols = telemetry.RootElement.GetProperty("protocols");

        foreach (var protocol in Protocols)
        {
            var workload = protocols.GetProperty(protocol).GetProperty("workload");
            Assert.Equal(targetCpuPercent, workload.GetProperty("target_cpu_percent").GetByte());
            Assert.Equal(targetDurationMs, workload.GetProperty("target_duration_ms").GetUInt64());
            Assert.True(workload.GetProperty("observed_duration_ms").GetUInt64() >= targetDurationMs);

            var actualCpuPercent = workload.GetProperty("observed_cpu_percent").GetDouble();
            Assert.True(Math.Abs(actualCpuPercent - targetCpuPercent) <= 1.0);
        }
    }
}

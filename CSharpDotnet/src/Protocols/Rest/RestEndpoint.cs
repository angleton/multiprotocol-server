using System.Diagnostics;

namespace MultiprotocolServer.Protocols.Rest;

public static class RestEndpoint
{
    public static async Task<string> HandleAsync(HttpRequest request, Telemetry telemetry)
    {
        var started = Stopwatch.StartNew();
        await Workload.RunFromHeadersAsync(request.Headers, telemetry);
        var payload = request.Query["payload"].ToString();
        var response = ProtocolResponse.For("REST");
        telemetry.Record(
            "rest",
            true,
            started.Elapsed,
            (ulong)payload.Length,
            (ulong)response.Length);
        return response;
    }
}

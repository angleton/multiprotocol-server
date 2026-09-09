using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MultiprotocolServer.Protocols.GraphQl;

public static class GraphQlTelemetryMiddleware
{
    public static RequestDelegate Create(RequestDelegate next, Telemetry telemetry) => async context =>
    {
        if (context.Request.Path != "/graphql" || context.Request.Method != HttpMethods.Post)
        {
            await next(context);
            return;
        }

        var started = Stopwatch.StartNew();
        await Workload.RunFromHeadersAsync(context.Request.Headers, telemetry);

        context.Request.EnableBuffering();
        using var requestBodyReader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var requestBody = await requestBodyReader.ReadToEndAsync();
        context.Request.Body.Position = 0;
        var requestBytes = (ulong)ExtractQueryLength(requestBody);

        var originalResponseBody = context.Response.Body;
        await using var bufferedResponseBody = new MemoryStream();
        context.Response.Body = bufferedResponseBody;

        await next(context);

        context.Response.Body = originalResponseBody;
        var responseBytes = bufferedResponseBody.ToArray();
        await originalResponseBody.WriteAsync(responseBytes);

        var success = !HasGraphQlErrors(responseBytes);
        telemetry.Record("graphql", success, started.Elapsed, requestBytes, (ulong)responseBytes.Length);
    };

    private static int ExtractQueryLength(string requestBody)
    {
        try
        {
            using var document = JsonDocument.Parse(requestBody);
            return document.RootElement.TryGetProperty("query", out var query) && query.ValueKind == JsonValueKind.String
                ? Encoding.UTF8.GetByteCount(query.GetString() ?? string.Empty)
                : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static bool HasGraphQlErrors(byte[] responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.TryGetProperty("errors", out var errors)
                   && errors.ValueKind == JsonValueKind.Array
                   && errors.GetArrayLength() > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace MultiprotocolServer.Protocols.WebSocketProtocol;

public static class WebSocketEndpoint
{
    public static async Task HandleAsync(HttpContext context, Telemetry telemetry)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var workload = Workload.FromHeaders(context.Request.Headers);
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await HandleSocketAsync(socket, telemetry, workload);
    }

    private static async Task HandleSocketAsync(
        WebSocket socket,
        Telemetry telemetry,
        (byte CpuPercent, ulong DurationMs)? workload)
    {
        var buffer = new byte[8192];

        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            using var messageStream = new MemoryStream();
            try
            {
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType != WebSocketMessageType.Close)
                    {
                        messageStream.Write(buffer, 0, result.Count);
                    }
                }
                while (!result.EndOfMessage);
            }
            catch (WebSocketException error)
            {
                Console.Error.WriteLine($"WebSocket receive failed: {error.Message}");
                break;
            }

            var started = Stopwatch.StartNew();
            switch (result.MessageType)
            {
                case WebSocketMessageType.Close:
                    return;
                case WebSocketMessageType.Text:
                {
                    var request = Encoding.UTF8.GetString(messageStream.ToArray());
                    if (workload is { } requestedWorkload)
                    {
                        telemetry.RecordWorkload(
                            await Workload.RunAsync(requestedWorkload.CpuPercent, requestedWorkload.DurationMs));
                    }

                    var response = ProtocolResponse.For("WebSocket");
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    var success = true;
                    try
                    {
                        await socket.SendAsync(responseBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (WebSocketException)
                    {
                        success = false;
                    }

                    // Record each client action as soon as its response is sent. While
                    // the server is running, /telemetry exposes this real-time
                    // WebSocket activity alongside the other protocols.
                    telemetry.Record(
                        "websocket",
                        success,
                        started.Elapsed,
                        (ulong)messageStream.Length,
                        success ? (ulong)responseBytes.Length : 0);

                    if (!success)
                    {
                        return;
                    }
                    break;
                }
                case WebSocketMessageType.Binary:
                    telemetry.Record("websocket", false, started.Elapsed, (ulong)messageStream.Length, 0);
                    await socket.CloseAsync(
                        WebSocketCloseStatus.InvalidMessageType,
                        "text messages are required",
                        CancellationToken.None);
                    return;
            }
        }
    }
}

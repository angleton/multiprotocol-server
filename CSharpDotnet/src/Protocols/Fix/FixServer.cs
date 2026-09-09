using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace MultiprotocolServer.Protocols.Fix;

public sealed class FixServer(IPEndPoint endpoint, Telemetry telemetry) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(endpoint);
        listener.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = HandleConnectionAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                await using var stream = client.GetStream();
                var request = await ReadLineAsync(stream, cancellationToken);

                var started = Stopwatch.StartNew();
                var workload = WorkloadFields(request);
                if (workload is { } requestedWorkload)
                {
                    telemetry.RecordWorkload(
                        await Workload.RunAsync(requestedWorkload.CpuPercent, requestedWorkload.DurationMs));
                }

                var success = IsHeartbeat(request);
                var response = success ? HeartbeatMessage() : RejectMessage();

                await stream.WriteAsync(response, cancellationToken);
                telemetry.Record("fix", success, started.Elapsed, (ulong)request.Length, (ulong)response.Length);
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"FIX connection failed: {error.Message}");
            }
        }
    }

    private static async Task<byte[]> ReadLineAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var singleByte = new byte[1];
        while (true)
        {
            var read = await stream.ReadAsync(singleByte, cancellationToken);
            if (read == 0)
            {
                break;
            }
            buffer.WriteByte(singleByte[0]);
            if (singleByte[0] == (byte)'\n')
            {
                break;
            }
        }
        return buffer.ToArray();
    }

    private static (byte CpuPercent, ulong DurationMs)? WorkloadFields(byte[] message)
    {
        byte? cpu = null;
        ulong? duration = null;

        foreach (var field in SplitFields(message))
        {
            var text = Encoding.UTF8.GetString(field);
            if (text.StartsWith("9000=", StringComparison.Ordinal) && byte.TryParse(text[5..], out var cpuValue))
            {
                cpu = cpuValue;
            }
            else if (text.StartsWith("9001=", StringComparison.Ordinal) && ulong.TryParse(text[5..], out var durationValue))
            {
                duration = durationValue;
            }
        }

        return cpu is null || duration is null ? null : Workload.FromValues(cpu.Value, duration.Value);
    }

    private static bool IsHeartbeat(byte[] message) =>
        SplitFields(message).Any(field => field.AsSpan().SequenceEqual("35=0"u8));

    private static IEnumerable<byte[]> SplitFields(byte[] message)
    {
        var start = 0;
        for (var i = 0; i < message.Length; i++)
        {
            if (message[i] == 1 || message[i] == (byte)'\n')
            {
                yield return message[start..i];
                start = i + 1;
            }
        }
        if (start < message.Length)
        {
            yield return message[start..];
        }
    }

    private static byte[] HeartbeatMessage() => BuildMessage("35=0\u0001"u8.ToArray());

    private static byte[] RejectMessage() => BuildMessage("35=3\u000158=Unsupported FIX message\u0001"u8.ToArray());

    private static byte[] BuildMessage(byte[] body)
    {
        var header = Encoding.UTF8.GetBytes($"8=FIX.4.4\u00019={body.Length}\u0001");
        var message = new byte[header.Length + body.Length];
        header.CopyTo(message, 0);
        body.CopyTo(message, header.Length);

        var checksum = 0;
        foreach (var b in message)
        {
            checksum += b;
        }
        checksum %= 256;

        var trailer = Encoding.UTF8.GetBytes($"10={checksum:D3}\u0001\n");
        var result = new byte[message.Length + trailer.Length];
        message.CopyTo(result, 0);
        trailer.CopyTo(result, message.Length);
        return result;
    }
}

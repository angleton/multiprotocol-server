using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace MultiprotocolServer;

public sealed record WorkloadSnapshot(
    [property: JsonPropertyName("target_cpu_percent")] byte TargetCpuPercent,
    [property: JsonPropertyName("target_duration_ms")] ulong TargetDurationMs,
    [property: JsonPropertyName("observed_duration_ms")] ulong ObservedDurationMs,
    [property: JsonPropertyName("observed_cpu_percent")] double ObservedCpuPercent);

public static class Workload
{
    private const string CpuHeader = "x-workload-cpu-percent";
    private const string DurationHeader = "x-workload-duration-ms";
    private const byte MaxCpuPercent = 100;
    private const ulong MaxDurationMs = 60_000;
    private const ulong SliceMs = 10;

    public static (byte CpuPercent, ulong DurationMs)? FromHeaders(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(CpuHeader, out var cpuValue) ||
            !headers.TryGetValue(DurationHeader, out var durationValue))
        {
            return null;
        }

        if (!byte.TryParse(cpuValue, out var cpuPercent) ||
            !ulong.TryParse(durationValue, out var durationMs))
        {
            return null;
        }

        return FromValues(cpuPercent, durationMs);
    }

    public static (byte CpuPercent, ulong DurationMs)? FromValues(byte cpuPercent, ulong durationMs)
    {
        return cpuPercent <= MaxCpuPercent && durationMs <= MaxDurationMs
            ? (cpuPercent, durationMs)
            : null;
    }

    public static async Task RunFromHeadersAsync(IHeaderDictionary headers, Telemetry telemetry)
    {
        var workload = FromHeaders(headers);
        if (workload is { } request)
        {
            telemetry.RecordWorkload(await RunAsync(request.CpuPercent, request.DurationMs));
        }
    }

    public static async Task<WorkloadSnapshot> RunAsync(byte cpuPercent, ulong durationMs)
    {
        var started = Stopwatch.StartNew();
        var target = TimeSpan.FromMilliseconds(durationMs);
        // Coarse OS timer wakeups add idle time on Windows, so compensate the
        // requested duty cycle while retaining the measured result in telemetry.
        var busyDuration = TimeSpan.FromMicroseconds(SliceMs * 1_000 * cpuPercent * 3 / 2 / 100.0);
        var idleDuration = TimeSpan.FromMilliseconds(SliceMs) - busyDuration;
        if (idleDuration < TimeSpan.Zero)
        {
            idleDuration = TimeSpan.Zero;
        }

        var busy = TimeSpan.Zero;

        while (started.Elapsed < target)
        {
            var busyStarted = Stopwatch.StartNew();
            var accumulator = 1UL;
            while (busyStarted.Elapsed < busyDuration)
            {
                accumulator = unchecked(accumulator * 31);
            }
            // Prevent the compiler from optimizing away the busy-wait loop above.
            if (accumulator == 0)
            {
                Console.Write(string.Empty);
            }
            busy += busyStarted.Elapsed < busyDuration ? busyStarted.Elapsed : busyDuration;

            if (idleDuration > TimeSpan.Zero)
            {
                await Task.Delay(idleDuration);
            }
        }

        var observedDuration = started.Elapsed;
        var observedCpuPercent = observedDuration == TimeSpan.Zero
            ? 0.0
            : busy.TotalSeconds / observedDuration.TotalSeconds * 100.0;

        return new WorkloadSnapshot(
            cpuPercent,
            durationMs,
            (ulong)observedDuration.TotalMilliseconds,
            observedCpuPercent);
    }
}

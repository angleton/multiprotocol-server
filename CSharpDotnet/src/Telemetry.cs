using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace MultiprotocolServer;

public sealed record ProtocolSnapshot(
    [property: JsonPropertyName("requests")] ulong Requests,
    [property: JsonPropertyName("failures")] ulong Failures,
    [property: JsonPropertyName("average_duration_us")] double AverageDurationUs,
    [property: JsonPropertyName("request_bytes")] ulong RequestBytes,
    [property: JsonPropertyName("response_bytes")] ulong ResponseBytes,
    [property: JsonPropertyName("workload")] WorkloadSnapshot? Workload);

public sealed record TelemetrySnapshot(
    [property: JsonPropertyName("protocols")] SortedDictionary<string, ProtocolSnapshot> Protocols);

public sealed class Telemetry
{
    private static readonly string[] Protocols = ["fix", "grpc", "graphql", "rest", "soap", "websocket"];

    private readonly ConcurrentDictionary<string, ProtocolTelemetry> _protocols = new();
    private readonly object _workloadLock = new();
    private WorkloadSnapshot? _workload;

    public void Record(string protocol, bool success, TimeSpan duration, ulong requestBytes, ulong responseBytes)
    {
        var entry = _protocols.GetOrAdd(protocol, _ => new ProtocolTelemetry());
        entry.Record(success, duration, requestBytes, responseBytes);
    }

    public void RecordWorkload(WorkloadSnapshot workload)
    {
        lock (_workloadLock)
        {
            _workload = workload;
        }
    }

    public TelemetrySnapshot Snapshot()
    {
        WorkloadSnapshot? workload;
        lock (_workloadLock)
        {
            workload = _workload;
        }

        var protocols = new SortedDictionary<string, ProtocolSnapshot>(StringComparer.Ordinal);
        foreach (var (protocol, metrics) in _protocols)
        {
            protocols[protocol] = metrics.ToSnapshot(workload);
        }

        foreach (var protocol in Protocols)
        {
            protocols.TryAdd(protocol, new ProtocolSnapshot(0, 0, 0.0, 0, 0, workload));
        }

        return new TelemetrySnapshot(protocols);
    }

    private sealed class ProtocolTelemetry
    {
        private readonly object _lock = new();
        private ulong _requests;
        private ulong _failures;
        private ulong _totalDurationNs;
        private ulong _requestBytes;
        private ulong _responseBytes;

        public void Record(bool success, TimeSpan duration, ulong requestBytes, ulong responseBytes)
        {
            lock (_lock)
            {
                _requests++;
                if (!success)
                {
                    _failures++;
                }
                _totalDurationNs += (ulong)(duration.Ticks * 100);
                _requestBytes += requestBytes;
                _responseBytes += responseBytes;
            }
        }

        public ProtocolSnapshot ToSnapshot(WorkloadSnapshot? workload)
        {
            lock (_lock)
            {
                var averageDurationUs = _requests == 0
                    ? 0.0
                    : _totalDurationNs / (double)_requests / 1_000.0;

                return new ProtocolSnapshot(
                    _requests,
                    _failures,
                    averageDurationUs,
                    _requestBytes,
                    _responseBytes,
                    workload);
            }
        }
    }
}

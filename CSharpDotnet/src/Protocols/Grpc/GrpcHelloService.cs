using System.Diagnostics;
using Grpc.Core;
using MultiprotocolServer.Protos;

namespace MultiprotocolServer.Protocols.Grpc;

public sealed class GrpcHelloService(Telemetry telemetry) : Hello.HelloBase
{
    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        var started = Stopwatch.StartNew();

        var cpuHeader = context.RequestHeaders.Get("x-workload-cpu-percent")?.Value;
        var durationHeader = context.RequestHeaders.Get("x-workload-duration-ms")?.Value;
        if (byte.TryParse(cpuHeader, out var cpu) && ulong.TryParse(durationHeader, out var duration))
        {
            var workload = Workload.FromValues(cpu, duration);
            if (workload is { } requestedWorkload)
            {
                telemetry.RecordWorkload(
                    await Workload.RunAsync(requestedWorkload.CpuPercent, requestedWorkload.DurationMs));
            }
        }

        var responseMessage = ProtocolResponse.For("gRPC");
        telemetry.Record(
            "grpc",
            true,
            started.Elapsed,
            (ulong)(request.Name.Length + request.Payload.Length),
            (ulong)responseMessage.Length);

        return new HelloReply { Message = responseMessage };
    }
}

# C#/.NET Multiprotocol Server

A .NET 8 port of the Rust reference server, demonstrating the same application-layer
communication technologies — REST, GraphQL, SOAP, gRPC, FIX, and WebSocket — behind a
shared telemetry and controlled-workload model, using ASP.NET Core and Reqnroll-based
BDD scenarios.

## Prerequisites

- .NET 8 SDK

## Run the server

```bash
cd src
dotnet run
```

REST, GraphQL, SOAP, WebSocket, `/health`, and `/telemetry` are hosted over HTTP/1.1 on
`127.0.0.1:8080`. gRPC is hosted over cleartext HTTP/2 on `127.0.0.1:8081` (same Kestrel
process, second endpoint). The FIX acceptor runs as a raw `TcpListener` background service
on `127.0.0.1:8082`.

## Protocols

| Protocol | Request | Success response |
| --- | --- | --- |
| FIX | TCP message with `35=0` on port `8082` | FIX 4.4 `Heartbeat` with `35=0` |
| gRPC | `Hello.SayHello` with `payload` on port `8081` | `HelloReply.Message = "gRPC message"` |
| GraphQL | `POST /graphql` with a `hello` query | JSON with `data.hello = "GraphQL message"` |
| REST | `GET /hello?payload=...` | HTTP `200` with `REST message` |
| SOAP | `POST /soap` with a SOAP XML `Envelope` and `PingRequest` body | XML `Envelope` containing `SOAP message` |
| WebSocket | Text message to `ws://127.0.0.1:8080/ws` | Text message `WebSocket message` |

The SOAP handler parses the envelope/body/operation the same way the Rust version does:
invalid SOAP-shaped input receives HTTP `400`; valid requests receive `text/xml`.

Useful manual checks while debugging:

```bash
curl http://127.0.0.1:8080/health
curl "http://127.0.0.1:8080/hello?payload=debug"
curl -X POST http://127.0.0.1:8080/graphql \
	-H "content-type: application/json" \
	-d '{"query":"{ hello }"}'
curl -X POST http://127.0.0.1:8080/soap \
	-H "content-type: text/xml" \
	--data-raw '<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><PingRequest><Message>debug</Message></PingRequest></soap:Body></soap:Envelope>'
curl http://127.0.0.1:8080/telemetry
```

The gRPC contract is in [src/Protos/hello.proto](src/Protos/hello.proto), matching the
Rust project's `proto/hello.proto`. FIX uses the SOH byte (`0x01`) between fields and a
newline to delimit messages, identical to the Rust implementation.

Every protocol handler accepts the same controlled-workload headers
(`x-workload-cpu-percent` / `x-workload-duration-ms`, or FIX tags `9000`/`9001`) so the
technologies can be benchmarked under the same simulated CPU load; results are recorded
per-protocol in `/telemetry`.

## BDD tests

The Gherkin `.feature` files in [features](features) are shared with the Rust project.
Step definitions live in [tests/Steps](tests/Steps) using Reqnroll (the actively
maintained SpecFlow successor) with xUnit as the test runner.

```bash
dotnet test tests/MultiprotocolServer.Tests.csproj
```

Each scenario starts the full server (`MultiprotocolHost.Build`) on the same fixed ports
as `dotnet run`, so scenarios run sequentially rather than in parallel — see
`AssemblyInfo.cs` for the `DisableTestParallelization` setting.

## Solution layout

- `src/` — the ASP.NET Core server (`MultiprotocolServer.csproj`)
- `features/` — Gherkin scenarios shared with the Rust implementation
- `tests/` — the Reqnroll + xUnit BDD test project (`MultiprotocolServer.Tests.csproj`)

Build everything with:

```bash
dotnet build MultiprotocolServer.sln
```

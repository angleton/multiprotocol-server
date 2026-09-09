using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MultiprotocolServer;
using MultiprotocolServer.Protocols.Fix;
using MultiprotocolServer.Protocols.GraphQl;
using MultiprotocolServer.Protocols.Grpc;
using MultiprotocolServer.Protocols.Rest;
using MultiprotocolServer.Protocols.Soap;
using MultiprotocolServer.Protocols.WebSocketProtocol;

if (Environment.GetEnvironmentVariable("MULTIPROTOCOL_SERVER_SKIP_AUTOSTART") != "1")
{
    var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
    var app = MultiprotocolHost.Build(endpoint);
    Console.WriteLine($"Server running at http://{endpoint}");
    await app.RunAsync();
}

namespace MultiprotocolServer
{
    public static class MultiprotocolHost
    {
        public static IPEndPoint GrpcEndpoint(IPEndPoint http) => new(http.Address, http.Port + 1);

        public static IPEndPoint FixEndpoint(IPEndPoint http) => new(http.Address, http.Port + 2);

        public static WebApplication Build(IPEndPoint httpEndpoint, Telemetry? telemetry = null)
        {
            telemetry ??= new Telemetry();
            var builder = WebApplication.CreateBuilder();

            builder.Services.AddSingleton(telemetry);
            builder.Services.AddGrpc();
            builder.Services.AddGraphQLServer().AddQueryType<QueryRoot>();
            builder.Services.AddHostedService(_ => new FixServer(FixEndpoint(httpEndpoint), telemetry));

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(httpEndpoint, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
                options.Listen(GrpcEndpoint(httpEndpoint), listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
            });

            var app = builder.Build();

            app.UseWebSockets();

            app.MapGet("/health", async context =>
            {
                var started = Stopwatch.StartNew();
                const string response = "ok";
                telemetry.Record("health", true, started.Elapsed, 0, (ulong)response.Length);
                await context.Response.WriteAsync(response);
            });

            app.MapGet("/hello", async context =>
            {
                var response = await RestEndpoint.HandleAsync(context.Request, telemetry);
                await context.Response.WriteAsync(response);
            });

            app.MapPost("/soap", context => SoapEndpoint.HandleAsync(context, telemetry));

            app.Map("/ws", context => WebSocketEndpoint.HandleAsync(context, telemetry));

            app.MapGet("/telemetry", () => telemetry.Snapshot());

            app.Use(next => GraphQlTelemetryMiddleware.Create(next, telemetry));
            app.MapGraphQL("/graphql");

            app.MapGrpcService<GrpcHelloService>();

            return app;
        }
    }
}

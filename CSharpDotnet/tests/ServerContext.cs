using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using MultiprotocolServer;

namespace MultiprotocolServer.Tests;

public sealed class ServerContext : IAsyncDisposable
{
    public const string HttpHost = "127.0.0.1";
    public const int HttpPort = 8080;

    public HttpClient Http { get; } = new();

    public WebApplication? App { get; private set; }

    public async Task StartAsync()
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(HttpHost), HttpPort);
        App = MultiprotocolHost.Build(endpoint);
        await App.StartAsync();
        // Give Kestrel a moment to finish binding all configured endpoints.
        await Task.Delay(500);
    }

    public async ValueTask DisposeAsync()
    {
        if (App is not null)
        {
            await App.StopAsync();
            await App.DisposeAsync();
            App = null;
        }

        Http.Dispose();
    }
}

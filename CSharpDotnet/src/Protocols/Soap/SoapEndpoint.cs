using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace MultiprotocolServer.Protocols.Soap;

public static class SoapEndpoint
{
    public static async Task HandleAsync(HttpContext context, Telemetry telemetry)
    {
        var started = Stopwatch.StartNew();
        await Workload.RunFromHeadersAsync(context.Request.Headers, telemetry);

        using var memoryStream = new MemoryStream();
        await context.Request.Body.CopyToAsync(memoryStream);
        var body = memoryStream.ToArray();

        var payload = TryReadPingPayload(body);
        if (payload is null)
        {
            telemetry.Record("soap", false, started.Elapsed, (ulong)body.Length, 0);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Request received, but it was not recognized as SOAP");
            return;
        }

        var responseBody = Acknowledgement(ProtocolResponse.For("SOAP"));
        telemetry.Record("soap", true, started.Elapsed, (ulong)body.Length, (ulong)responseBody.Length);
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/xml; charset=utf-8";
        await context.Response.WriteAsync(responseBody);
    }

    private static string? TryReadPingPayload(byte[] body)
    {
        XDocument document;
        try
        {
            using var stream = new MemoryStream(body);
            document = XDocument.Load(stream);
        }
        catch (Exception)
        {
            return null;
        }

        var root = document.Root;
        if (root is null || root.Name.LocalName != "Envelope")
        {
            return null;
        }

        var soapBody = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Body");
        var pingRequest = soapBody?.Elements().FirstOrDefault(e => e.Name.LocalName == "PingRequest");
        if (pingRequest is null)
        {
            return null;
        }

        var message = pingRequest.Elements().FirstOrDefault(e => e.Name.LocalName == "Message");
        return message?.Value ?? string.Empty;
    }

    private static string Acknowledgement(string message) =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <PingResponse>
              <Message>{message}</Message>
            </PingResponse>
          </soap:Body>
        </soap:Envelope>
        """;
}

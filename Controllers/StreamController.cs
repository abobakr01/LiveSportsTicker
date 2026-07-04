using System.Text.Json;
using System.Text.Json.Serialization;
using LiveSportsTicker.Models;
using LiveSportsTicker.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiveSportsTicker.Controllers;

public class StreamController : Controller
{
    private readonly MatchSimulator _simulator;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public StreamController(MatchSimulator simulator)
    {
        _simulator = simulator;
    }

    /// <summary>
    /// GET /Stream/Live
    /// A single, continuously open HTTP response using the Server-Sent Events protocol.
    /// - Streams every new match event the instant it happens (no polling from the browser).
    /// - Supports reconnection: if the browser sends "Last-Event-ID", we first replay
    ///   everything the client missed before continuing with the live feed, so a killed
    ///   connection resumes cleanly instead of losing history.
    /// - Sends periodic ":heartbeat" comments so proxies/browsers don't time the connection out
    ///   and so a dead connection is detected quickly.
    /// </summary>
    [HttpGet("Stream/Live")]
    public async Task Live()
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // disable nginx buffering if ever deployed behind it

        var cancellationToken = HttpContext.RequestAborted;

        long lastSentId = 0;
        // Native browsers send this header automatically on an *automatic* EventSource
        // reconnect. We also accept it as a query string so the demo "kill connection"
        // button can force a manual reconnect that still resumes from the right place.
        if (Request.Headers.TryGetValue("Last-Event-ID", out var lastEventIdHeader) &&
            long.TryParse(lastEventIdHeader, out var parsedId))
        {
            lastSentId = parsedId;
        }
        else if (long.TryParse(Request.Query["lastEventId"], out var parsedQueryId))
        {
            lastSentId = parsedQueryId;
        }

        try
        {
            // 1) Reconnect support: replay anything the client missed while disconnected.
            var missed = _simulator.GetEventsSince(lastSentId);
            foreach (var evt in missed)
            {
                await WriteEventAsync(evt, cancellationToken);
                lastSentId = evt.Id;
            }

            // 2) Continue streaming live, forever, until the client disconnects.
            while (!cancellationToken.IsCancellationRequested)
            {
                var newEvents = _simulator.GetEventsSince(lastSentId);

                if (newEvents.Count > 0)
                {
                    foreach (var evt in newEvents)
                    {
                        await WriteEventAsync(evt, cancellationToken);
                        lastSentId = evt.Id;
                    }
                }
                else
                {
                    await Response.WriteAsync(":heartbeat\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal: the browser tab closed, navigated away, or the connection was killed
            // deliberately for the reconnect demo. Nothing to do - just let the response end.
        }
    }

    private async Task WriteEventAsync(MatchEvent evt, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(evt, JsonOptions);
        var message = $"id: {evt.Id}\nevent: matchEvent\ndata: {payload}\n\n";
        await Response.WriteAsync(message, ct);
        await Response.Body.FlushAsync(ct);
    }
}

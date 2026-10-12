
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// Railway supplies PORT. Locally, use port 8080 by default.
var port = Environment.GetEnvironmentVariable("PORT") ?? "55555";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/health", () => "Claim the City server is running.");

app.Run(async context => {
    if (context.Request.Path != "/" ||
        !context.WebSockets.IsWebSocketRequest) {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync(
            "Expected a WebSocket connection at /");
        return;
    }

    using var socket =
        await context.WebSockets.AcceptWebSocketAsync();

    Console.WriteLine("WebSocket client connected.");

    var buffer = new byte[8192];

    try {
        while (socket.State == WebSocketState.Open) {
            using var message = new MemoryStream();
            WebSocketReceiveResult result;

            do {
                result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close) {
                    break;
                }

                message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            Console.WriteLine(
                $"Received {message.Length} bytes.");

            // Game messages will be handled here next.
        }
    } catch (WebSocketException ex) {
        Console.WriteLine($"WebSocket disconnected: {ex.Message}");
    } finally {
        Console.WriteLine("WebSocket client disconnected.");
    }
});
await app.RunAsync();
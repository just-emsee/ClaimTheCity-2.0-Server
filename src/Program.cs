using System.Net.WebSockets;
using shared;

// Railway provides PORT. Use 55555 when running locally.
var port = Environment.GetEnvironmentVariable("PORT") ?? "55555";
var _adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin";
var _savePath = Environment.GetEnvironmentVariable("SAVE_PATH") ?? "./save";
Server.SetAdminPass(_adminPassword);
Server.savePath = _savePath;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions {
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

Server.loadSaveData(Server.savePath);

// Local console commands are optional. Enable them with
// ENABLE_CONSOLE_COMMANDS=true when running locally.
if (Environment.GetEnvironmentVariable("ENABLE_CONSOLE_COMMANDS") == "true") {
    _ = Task.Run(Server.ConsoleCommandLoopAsync);
}

app.MapGet("/health", () => "Claim the City server is running.");

app.Run(async context => {
    if (context.Request.Path != "/" ||
        !context.WebSockets.IsWebSocketRequest) {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync(
            "Expected a WebSocket connection at /");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var client = Server.AddClient(socket);

    Console.WriteLine($"Client {client.Id} connected.");

    try {
        // Tell Unity that the connection has been accepted.
        await Server.SendMessageToClient(
            client,
            new AcceptClientMessage(client.Id));

        var buffer = new byte[8192];

        while (socket.State == WebSocketState.Open) {
            using var message = new MemoryStream();
            WebSocketReceiveResult result;

            // A WebSocket message can arrive in multiple fragments.
            do {
                result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType != WebSocketMessageType.Binary)
                    throw new InvalidDataException(
                        "Only binary messages are supported.");

                if (message.Length + result.Count > 1024 * 1024)
                    throw new InvalidDataException(
                        "Message exceeds the 1 MB limit.");

                message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            byte[] bytes = message.ToArray();

            try {
                var packet = new Packet(bytes);
                ISerializable incoming = packet.ReadObject();

                if (incoming == null)
                    throw new InvalidDataException(
                        "Could not deserialize message.");

                //Console.WriteLine(
                //    $"Client {client.Id}: {incoming.GetType().Name}");

                ISerializable response =
                    MessageHandler.HandleMessage(client, incoming);

                Server.CheckSaveDataDirty();

                if (response != null) {
                    await Server.SendMessageToClient(client, response);
                }
            } catch (Exception ex) {
                // Reject this message, but keep the connection alive.
                Console.WriteLine(
                    $"Invalid message from client {client.Id}: {ex.Message}");
            }
        }
    } catch (WebSocketException ex) {
        Console.WriteLine(
            $"WebSocket error for client {client.Id}: {ex.Message}");
    } catch (Exception ex) {
        Console.WriteLine(
            $"Client {client.Id} error: {ex.Message}");
    } finally {
        Server.RemoveClient(client);

        if (socket.State == WebSocketState.Open ||
            socket.State == WebSocketState.CloseReceived) {
            try {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None);
            } catch (WebSocketException) {
                // The remote client may already have disconnected.
            }
        }

        Console.WriteLine($"Client {client.Id} disconnected.");
    }
});

await app.RunAsync();


// --------------------------------------------------
// Server logic
// --------------------------------------------------

public static class Server {
    public static readonly object GameDataLock = new();
    public static readonly object ClientsLock = new();

    public static readonly List<ClientConnection> clients = new();

    private static int nextClientId = 1;

    public static GameData _gameData = new GameData();

    internal static string savePath = "./save";

    public static string adminPassword = "admin";

    public static ClientConnection AddClient(WebSocket socket) {
        var client = new ClientConnection(
            Interlocked.Increment(ref nextClientId) - 1,
            socket);

        lock (ClientsLock) {
            clients.Add(client);
        }

        return client;
    }

    public static void RemoveClient(ClientConnection client) {
        lock (ClientsLock) {
            clients.Remove(client);
        }

        client.Dispose();
    }

    public static async Task SendMessageToClient(
        ClientConnection client,
        ISerializable message) {
        byte[] bytes;

        // Serialize while game state is protected from concurrent changes.
        lock (GameDataLock) {
            var packet = new Packet();
            packet.Write(message);
            bytes = packet.GetBytes();
        }

        await client.SendBytesAsync(bytes);
    }

    public static void SendGameUpdateToAll() {
        _ = BroadcastGameUpdateAsync();
    }

    private static async Task BroadcastGameUpdateAsync() {
        byte[] bytes;

        lock (GameDataLock) {
            var packet = new Packet();
            packet.Write(new DataUpdateMessage(_gameData));
            bytes = packet.GetBytes();
        }

        ClientConnection[] snapshot;

        lock (ClientsLock) {
            snapshot = clients.ToArray();
        }

        var sends = snapshot.Select(async client => {
            try {
                await client.SendBytesAsync(bytes);
            } catch (Exception ex) {
                Console.WriteLine(
                    $"Could not send update to client {client.Id}: {ex.Message}");
            }
        });

        await Task.WhenAll(sends);
    }

    public static void loadSaveData(string path) {
        if (!File.Exists(path)) {
            Console.WriteLine("No save file found; starting with empty game data.");
            return;
        }

        try {
            byte[] data = File.ReadAllBytes(path);
            var packet = new Packet(data);
            var loadedData = (GameData)packet.ReadObject();

            lock (GameDataLock) {
                _gameData = loadedData;
            }

            Console.WriteLine("Game data loaded.");
        } catch (Exception ex) {
            Console.WriteLine($"Could not load save data: {ex.Message}");
            throw;
        }
    }

    public static void saveGameData(string path) {
        lock (GameDataLock) {
            var packet = new Packet();
            packet.Write(_gameData);

            string? directory = Path.GetDirectoryName(
                Path.GetFullPath(path));

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(path, packet.GetBytes());
            _gameData.updated = false;
        }
    }

    public static void CheckSaveDataDirty() {
        lock (GameDataLock) {
            if (!_gameData.updated)
                return;

            Console.WriteLine("Saving game data...");
            saveGameData(savePath);
            Console.WriteLine("Save complete.");
        }
    }

    public static async Task ConsoleCommandLoopAsync() {
        while (true) {
            string? input = Console.ReadLine();

            if (input == null)
                return;

            try {
                lock (GameDataLock) {
                    ConsoleCommands.HandleCommand(input);
                }

                CheckSaveDataDirty();
            } catch (Exception ex) {
                Console.WriteLine($"Command failed: {ex.Message}");
            }

            await Task.Yield();
        }
    }

    public static void SetAdminPass(string adminPassword) {
        Server.adminPassword = adminPassword;
    }
}


// --------------------------------------------------
// Connected client
// --------------------------------------------------

public sealed class ClientConnection : IDisposable {
    private readonly SemaphoreSlim sendLock = new(1, 1);

    public int Id { get; }
    public WebSocket Socket { get; }

    public string? TeamPassword { get; set; }

    public ClientConnection(int id, WebSocket socket) {
        Id = id;
        Socket = socket;
    }

    public async Task SendBytesAsync(byte[] bytes) {
        await sendLock.WaitAsync();

        try {
            if (Socket.State != WebSocketState.Open)
                return;

            await Socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Binary,
                endOfMessage: true,
                cancellationToken: CancellationToken.None);
        } finally {
            sendLock.Release();
        }
    }

    public void Dispose() {
        TeamPassword = null;
        sendLock.Dispose();
    }
}
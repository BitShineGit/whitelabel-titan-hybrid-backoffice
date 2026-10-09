using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class WebSocketHandler
{
    private readonly WebSocketConnectionManager _manager;

    public WebSocketHandler(WebSocketConnectionManager manager)
    {
        _manager = manager;
    }

    public async Task HandleAsync(WebSocket socket)
    {
        var socketId = _manager.AddSocket(socket);
        var buffer = new byte[1024 * 4];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msgText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<Dictionary<string, string>>(msgText);

                    if (message != null &&
                        message.TryGetValue("type", out var type) && type == "register" &&
                        message.TryGetValue("userId", out var userId))
                    {
                        _manager.AddUser(userId, socketId);
                        Console.WriteLine($"User registered: {userId} -> {socketId}");
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _manager.RemoveSocket(socketId);
                    break;
                }
            }
        }
        finally
        {
            // Belt-and-braces: catches abrupt disconnects that never send a Close frame.
            await _manager.RemoveSocket(socketId);
        }
    }

    public Task SendToUserAsync<T>(string userId, T payload) => SendToUserAsync(userId, JsonSerializer.Serialize(payload));

    public async Task SendToUserAsync(string userId, string message)
    {
        var socketIds = _manager.GetSocketIdsByUserId(userId).ToList();
        if (socketIds.Count == 0)
        {
            Console.WriteLine("User doesn't exist.");
            return;
        }

        var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));

        foreach (var socketId in socketIds)
        {
            var socket = _manager.GetSocketById(socketId);
            if (socket == null || socket.State != WebSocketState.Open)
            {
                continue;
            }

            try
            {
                await _manager.SendAsync(socketId, socket, bytes, WebSocketMessageType.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send to {socketId} failed: {ex.Message}");
                await _manager.RemoveSocket(socketId);
            }
        }
    }

    // Sends the same message to every currently-connected socket.
    public async Task BroadcastAsync(string message, CancellationToken ct = default)
    {
        var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));

        var sendTasks = _manager.GetAllSockets()
            .Select(kv => SendSafeAsync(kv.Key, kv.Value, bytes, ct));

        await Task.WhenAll(sendTasks);
    }

    // Convenience overload — e.g. handler.BroadcastAsync(new { type = "jackpotUpdated", data = updates });
    public Task BroadcastAsync<T>(T payload, CancellationToken ct = default) =>
        BroadcastAsync(JsonSerializer.Serialize(payload), ct);

    private async Task SendSafeAsync(string socketId, WebSocket socket, ArraySegment<byte> bytes, CancellationToken ct)
    {
        try
        {
            await _manager.SendAsync(socketId, socket, bytes, WebSocketMessageType.Text, ct);
        }
        catch (Exception ex)
        {
            // Don't let one dead connection fail the whole broadcast.
            Console.WriteLine($"Broadcast to {socketId} failed: {ex.Message}");
            await _manager.RemoveSocket(socketId);
        }
    }
}
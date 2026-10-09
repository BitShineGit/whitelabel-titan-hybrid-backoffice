using Microsoft.Identity.Client;
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

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var msgText = Encoding.UTF8.GetString(buffer, 0, result.Count);

                var message = JsonSerializer.Deserialize<Dictionary<string, string>>(msgText);
                if (message != null && message.ContainsKey("type"))
                {
                    if (message["type"] == "register")
                    {
                        string userId = message["userId"];
                        _manager.AddUser(userId, socketId);
                        Console.WriteLine($"User registered: {userId} -> {socketId}");
                    }
                }
            }



            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _manager.RemoveSocket(socketId);
            }

        }
    }

    public async Task SendToUserAsync(string userId, string message)
    {
        var socket = _manager.GetSocketByUserId(userId);
        if (socket == null)
        {
            Console.WriteLine("User doesn't exist.");
            return;
        }
        else
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

    }
}
using System.Net.WebSockets;
using System.Collections.Concurrent;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
    private readonly ConcurrentDictionary<string, string> _userMap = new();
    // userId → socketId

    public string AddSocket(WebSocket socket)
    {
        string socketId = Guid.NewGuid().ToString();
        _sockets.TryAdd(socketId, socket);
        return socketId;
    }

    public void AddUser(string userId, string socketId)
    {
        _userMap[userId] = socketId;
    }

    public WebSocket? GetSocketByUserId(string userId)
    {
        if (_userMap.TryGetValue(userId, out var socketId))
        {
            if (_sockets.TryGetValue(socketId, out var socket))
                return socket;
        }
        return null;
    }

    public IEnumerable<WebSocket> GetAllSockets() => _sockets.Values;

    public async Task RemoveSocket(string socketId)
    {
        if (_sockets.TryRemove(socketId, out var socket))
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
        }

        foreach (var kv in _userMap.Where(x => x.Value == socketId).ToList())
        {
            _userMap.TryRemove(kv.Key, out _);
        }
    }
}
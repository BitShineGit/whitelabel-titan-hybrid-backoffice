using System.Net.WebSockets;
using System.Collections.Concurrent;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sendLocks = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userMap = new();

    public string AddSocket(WebSocket socket)
    {
        string socketId = Guid.NewGuid().ToString();
        _sockets.TryAdd(socketId, socket);
        _sendLocks.TryAdd(socketId, new SemaphoreSlim(1, 1));
        return socketId;
    }

    public void AddUser(string userId, string socketId)
    {
        var sockets = _userMap.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        sockets.TryAdd(socketId, 0);
    }

    public WebSocket? GetSocketById(string socketId) =>
        _sockets.TryGetValue(socketId, out var socket) ? socket : null;

    public IEnumerable<string> GetSocketIdsByUserId(string userId) =>
        _userMap.TryGetValue(userId, out var sockets) ? sockets.Keys.ToList() : Enumerable.Empty<string>();

    public IEnumerable<KeyValuePair<string, WebSocket>> GetAllSockets() => _sockets;

    // All sends go through here so a broadcast and a direct message to the
    // same connection can never collide (WebSocket.SendAsync isn't reentrant).
    public async Task SendAsync(string socketId, WebSocket socket, ArraySegment<byte> bytes,
        WebSocketMessageType messageType, CancellationToken ct = default)
    {
        if (socket.State != WebSocketState.Open)
            return;

        var gate = _sendLocks.GetOrAdd(socketId, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(ct);
        try
        {
            if (socket.State == WebSocketState.Open)
                await socket.SendAsync(bytes, messageType, true, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RemoveSocket(string socketId)
    {
        if (_sockets.TryRemove(socketId, out var socket))
        {
            try
            {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
            }
            catch
            {
                // Socket may already be aborted/disposed on the client side — nothing to do.
            }
        }

        if (_sendLocks.TryRemove(socketId, out var gate))
            gate.Dispose();

        // Remove this socketId from whichever user(s) it's registered under,
        // and clean up the user entry entirely if they have no sockets left.
        foreach (var kv in _userMap)
        {
            if (kv.Value.TryRemove(socketId, out _) && kv.Value.IsEmpty)
            {
                _userMap.TryRemove(kv.Key, out _);
            }
        }
    }
}
using NetworkDefinition.ErrorCode;
using RTWServer.Game.Player;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public class ChatHandler : IChatHandler
{
    private readonly IChatRoomManager _roomManager;

    public ChatHandler(IChatRoomManager roomManager)
    {
        _roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
    }

    public async Task<bool> HandleRoomBroadcastAsync(string roomId, IPacket packet, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return false;
        }

        if (packet == null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        var room = _roomManager.GetRoom(roomId);
        if (room == null)
        {
            return false;
        }

        await room.BroadcastAsync(packet, token).ConfigureAwait(false);
        return true;
    }

    public Task<RTWErrorCode> JoinRoomAsync(string roomId, IPlayer player)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return Task.FromResult(RTWErrorCode.InvalidRequest);
        }

        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        var room = _roomManager.GetRoom(roomId);
        if (room == null)
        {
            _roomManager.CreateRoom(roomId, roomId);
        }

        bool joined = _roomManager.JoinRoom(roomId, player);
        return Task.FromResult(joined ? RTWErrorCode.Success : RTWErrorCode.InvalidOperation);
    }

    public Task<RTWErrorCode> LeaveRoomAsync(string roomId, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return Task.FromResult(RTWErrorCode.InvalidRequest);
        }

        bool left = _roomManager.LeaveRoom(roomId, sessionId);
        return Task.FromResult(left ? RTWErrorCode.Success : RTWErrorCode.InvalidOperation);
    }

    public int CleanupSession(string sessionId)
    {
        return _roomManager.LeaveAllRooms(sessionId);
    }

    public bool IsMember(string roomId, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        var room = _roomManager.GetRoom(roomId);
        return room != null && room.ContainsMember(sessionId);
    }
}

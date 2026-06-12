using NetworkDefinition.ErrorCode;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Game.Player;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public class ChatService : IChatService
{
    private readonly IChatRoomManager _roomManager;

    public ChatService(IChatRoomManager roomManager)
    {
        _roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
    }

    public async Task<RTWErrorCode> SendChatMessageAsync(string roomId, string sessionId, string message, uint chatType = 0, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(sessionId))
        {
            return RTWErrorCode.InvalidRequest;
        }

        var room = _roomManager.GetRoom(roomId);
        if (room == null || !room.TryGetMember(sessionId, out var sender) || sender == null)
        {
            return RTWErrorCode.InvalidOperation;
        }

        var sChat = new SChat
        {
            ChatType = chatType,
            SenderPlayerId = sender.PlayerId,
            SenderName = sender.Name,
            Message = message ?? string.Empty
        };

        var packet = new ProtoPacket(PacketId.SChat, sChat);
        await room.BroadcastAsync(packet, token);
        return RTWErrorCode.Success;
    }

    public async Task<bool> BroadcastPacketAsync(string roomId, IPacket packet, CancellationToken token = default)
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

        await room.BroadcastAsync(packet, token);
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

        // 이미 존재하면 기존 방 반환. roomId가 너무 길거나 방 개수 제한을 넘으면 null.
        var room = _roomManager.GetOrCreateRoom(roomId, roomId);
        if (room == null)
        {
            return Task.FromResult(RTWErrorCode.InvalidRequest);
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

using System.Collections.Concurrent;
using RTWServer.Game.Player;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public class ChatRoomManager : IChatRoomManager
{
    private readonly ConcurrentDictionary<string, IChatRoom> _rooms = new();
    private readonly Func<string, IClientSession?> _sessionResolver;

    public ChatRoomManager(Func<string, IClientSession?> sessionResolver)
    {
        _sessionResolver = sessionResolver ?? throw new ArgumentNullException(nameof(sessionResolver));
    }

    public IChatRoom CreateRoom(string roomId, string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            throw new ArgumentException("RoomId cannot be null or whitespace.", nameof(roomId));
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            throw new ArgumentException("RoomName cannot be null or whitespace.", nameof(roomName));
        }

        return _rooms.GetOrAdd(roomId, id => new ChatRoom(id, roomName, _sessionResolver));
    }

    public bool RemoveRoom(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return false;
        }

        return _rooms.TryRemove(roomId, out _);
    }

    public IChatRoom? GetRoom(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return null;
        }

        _rooms.TryGetValue(roomId, out var room);
        return room;
    }

    public IReadOnlyCollection<IChatRoom> GetAllRooms()
    {
        return _rooms.Values.ToArray();
    }

    public bool JoinRoom(string roomId, IPlayer player)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        var room = GetRoom(roomId);
        if (room == null)
        {
            return false;
        }

        return room.TryAddMember(player);
    }

    public bool LeaveRoom(string roomId, string sessionId)
    {
        var room = GetRoom(roomId);
        if (room == null)
        {
            return false;
        }

        return room.RemoveMember(sessionId);
    }

    public int LeaveAllRooms(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return 0;
        }

        int removed = 0;
        foreach (var room in _rooms.Values)
        {
            if (room.RemoveMember(sessionId))
            {
                removed++;
            }
        }

        return removed;
    }
}

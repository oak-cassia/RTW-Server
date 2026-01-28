using System.Collections.Concurrent;

namespace RTWServer.Game.Chat;

public class ChatRoomManager : IChatRoomManager
{
    private readonly ConcurrentDictionary<string, IChatRoom> _rooms = new();

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

        return _rooms.GetOrAdd(roomId, id => new ChatRoom(id, roomName));
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
}

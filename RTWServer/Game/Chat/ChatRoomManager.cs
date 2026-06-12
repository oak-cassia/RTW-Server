using RTWServer.Game.Player;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public class ChatRoomManager : IChatRoomManager
{
    // 클라이언트가 임의의 roomId로 방을 만들 수 있으므로 무한 증식을 막기 위한 상한
    private const int MAX_ROOMS = 1000;
    private const int MAX_ROOM_ID_LENGTH = 64;

    // 방 생성/참가/퇴장과 빈 방 정리가 경합하지 않도록 단일 락으로 보호한다
    private readonly Lock _lock = new();
    private readonly Dictionary<string, IChatRoom> _rooms = new();
    private readonly HashSet<string> _persistentRoomIds = new();
    private readonly Func<string, IClientSession?> _sessionResolver;

    public ChatRoomManager(Func<string, IClientSession?> sessionResolver)
    {
        _sessionResolver = sessionResolver ?? throw new ArgumentNullException(nameof(sessionResolver));
    }

    public IChatRoom? GetOrCreateRoom(string roomId, string roomName, bool isPersistent = false)
    {
        if (string.IsNullOrWhiteSpace(roomId) || roomId.Length > MAX_ROOM_ID_LENGTH)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            return null;
        }

        lock (_lock)
        {
            if (_rooms.TryGetValue(roomId, out var existing))
            {
                if (isPersistent)
                {
                    _persistentRoomIds.Add(roomId);
                }

                return existing;
            }

            if (_rooms.Count >= MAX_ROOMS)
            {
                return null;
            }

            var room = new ChatRoom(roomId, roomName, _sessionResolver);
            _rooms[roomId] = room;
            if (isPersistent)
            {
                _persistentRoomIds.Add(roomId);
            }

            return room;
        }
    }

    public bool RemoveRoom(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return false;
        }

        lock (_lock)
        {
            _persistentRoomIds.Remove(roomId);
            return _rooms.Remove(roomId);
        }
    }

    public IChatRoom? GetRoom(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return null;
        }

        lock (_lock)
        {
            return _rooms.GetValueOrDefault(roomId);
        }
    }

    public IReadOnlyCollection<IChatRoom> GetAllRooms()
    {
        lock (_lock)
        {
            return _rooms.Values.ToArray();
        }
    }

    public bool JoinRoom(string roomId, IPlayer player)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out var room))
            {
                return false;
            }

            return room.TryAddMember(player);
        }
    }

    public bool LeaveRoom(string roomId, string sessionId)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out var room))
            {
                return false;
            }

            bool left = room.RemoveMember(sessionId);
            if (left)
            {
                RemoveRoomIfEmpty(room);
            }

            return left;
        }
    }

    public int LeaveAllRooms(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return 0;
        }

        lock (_lock)
        {
            int removed = 0;
            foreach (var room in _rooms.Values.ToArray())
            {
                if (room.RemoveMember(sessionId))
                {
                    removed++;
                    RemoveRoomIfEmpty(room);
                }
            }

            return removed;
        }
    }

    // 호출 전 _lock을 잡고 있어야 한다
    private void RemoveRoomIfEmpty(IChatRoom room)
    {
        if (room.MemberCount == 0 && !_persistentRoomIds.Contains(room.Id))
        {
            _rooms.Remove(room.Id);
        }
    }
}

using System.Collections.Concurrent;
using RTWServer.Game.Player;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public class ChatRoom : IChatRoom
{
    private readonly ConcurrentDictionary<string, IPlayer> _members = new();

    public ChatRoom(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }
    public string Name { get; }
    public int MemberCount => _members.Count;

    public bool TryAddMember(IPlayer player)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        return _members.TryAdd(player.SessionId, player);
    }

    public bool RemoveMember(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        return _members.TryRemove(sessionId, out _);
    }

    public bool ContainsMember(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        return _members.ContainsKey(sessionId);
    }

    public IReadOnlyCollection<IPlayer> GetMembers()
    {
        return _members.Values.ToArray();
    }

    public async Task BroadcastAsync(IPacket packet, CancellationToken token = default)
    {
        if (packet == null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if (_members.IsEmpty)
        {
            return;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RTWServer.Game.Player;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public class ChatRoom : IChatRoom
{
    private readonly ConcurrentDictionary<string, IPlayer> _members = new();
    private readonly Func<string, IClientSession?> _sessionResolver;
    private readonly ILogger<ChatRoom>? _logger;

    public ChatRoom(string id, string name, Func<string, IClientSession?> sessionResolver, ILogger<ChatRoom>? logger = null)
    {
        Id = id;
        Name = name;
        _sessionResolver = sessionResolver ?? throw new ArgumentNullException(nameof(sessionResolver));
        _logger = logger;
    }

    public string Id { get; }
    public string Name { get; }
    public int MemberCount => _members.Count;

    public bool TryAddMember(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return _members.TryAdd(player.SessionId, player);
    }

    public bool RemoveMember(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        return _members.TryRemove(sessionId, out _);
    }

    public bool ContainsMember(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        return _members.ContainsKey(sessionId);
    }

    public IReadOnlyCollection<IPlayer> GetMembers() => _members.Values.ToArray();

    public async Task BroadcastAsync(IPacket packet, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (_members.IsEmpty)
            return;

        var members = _members.Values.ToArray();
        if (members.Length == 0)
            return;

        var sendTasks = new List<Task>(members.Length);
        foreach (var member in members)
        {
            if (token.IsCancellationRequested)
                break;

            var session = _sessionResolver(member.SessionId);
            if (session != null)
            {
                sendTasks.Add(SendToSessionAsync(session, packet, member.SessionId));
            }
        }

        if (sendTasks.Count > 0)
        {
            await Task.WhenAll(sendTasks).ConfigureAwait(false);
        }
    }

    private async Task SendToSessionAsync(IClientSession session, IPacket packet, string sessionId)
    {
        try
        {
            await session.SendAsync(packet).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send packet to session {SessionId} in room {RoomId}", sessionId, Id);
        }
    }
}
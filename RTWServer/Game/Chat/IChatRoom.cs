using RTWServer.Game.Player;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public interface IChatRoom
{
    string Id { get; }
    string Name { get; }
    int MemberCount { get; }

    bool TryAddMember(IPlayer player);
    bool RemoveMember(string sessionId);
    bool ContainsMember(string sessionId);
    IReadOnlyCollection<IPlayer> GetMembers();

    Task BroadcastAsync(IPacket packet, CancellationToken token = default);
}

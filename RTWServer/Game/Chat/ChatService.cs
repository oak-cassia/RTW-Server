using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public class ChatService : IChatService
{
    private readonly Func<string, IClientSession?> _sessionResolver;

    public ChatService(Func<string, IClientSession?> sessionResolver)
    {
        _sessionResolver = sessionResolver ?? throw new ArgumentNullException(nameof(sessionResolver));
    }

    public async Task BroadcastToRoomAsync(IChatRoom room, IPacket packet, CancellationToken token = default)
    {
        if (room == null)
        {
            throw new ArgumentNullException(nameof(room));
        }

        if (packet == null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        var members = room.GetMembers();
        if (members.Count == 0)
        {
            return;
        }

        List<Task> sendTasks = new(members.Count);
        foreach (var member in members)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var session = _sessionResolver(member.SessionId);
            if (session != null)
            {
                sendTasks.Add(session.SendAsync(packet));
            }
        }

        if (sendTasks.Count > 0)
        {
            await Task.WhenAll(sendTasks).ConfigureAwait(false);
        }
    }
}

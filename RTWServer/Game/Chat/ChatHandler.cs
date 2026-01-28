using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public class ChatHandler : IChatHandler
{
    private readonly IChatRoomManager _roomManager;
    private readonly IChatService _chatService;

    public ChatHandler(IChatRoomManager roomManager, IChatService chatService)
    {
        _roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
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

        await _chatService.BroadcastToRoomAsync(room, packet, token).ConfigureAwait(false);
        return true;
    }
}

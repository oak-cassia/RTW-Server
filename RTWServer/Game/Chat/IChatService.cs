using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public interface IChatService
{
    Task BroadcastToRoomAsync(IChatRoom room, IPacket packet, CancellationToken token = default);
}

using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public interface IChatHandler
{
    Task<bool> HandleRoomBroadcastAsync(string roomId, IPacket packet, CancellationToken token = default);
}

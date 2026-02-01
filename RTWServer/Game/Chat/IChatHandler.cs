using NetworkDefinition.ErrorCode;
using RTWServer.Game.Player;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public interface IChatHandler
{
    Task<bool> HandleRoomBroadcastAsync(string roomId, IPacket packet, CancellationToken token = default);
    Task<RTWErrorCode> JoinRoomAsync(string roomId, IPlayer player);
    Task<RTWErrorCode> LeaveRoomAsync(string roomId, string sessionId);
    int CleanupSession(string sessionId);
    bool IsMember(string roomId, string sessionId);
}
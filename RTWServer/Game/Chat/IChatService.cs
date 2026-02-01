using NetworkDefinition.ErrorCode;
using RTWServer.Game.Player;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Chat;

public interface IChatService
{
    /// <summary>
    /// 채팅 메시지를 처리하고 해당 방에 브로드캐스트합니다.
    /// </summary>
    Task<RTWErrorCode> SendChatMessageAsync(string roomId, string sessionId, string senderName, string message, uint chatType = 0, CancellationToken token = default);
    
    /// <summary>
    /// 이미 생성된 패킷을 방에 브로드캐스트합니다.
    /// </summary>
    Task<bool> BroadcastPacketAsync(string roomId, IPacket packet, CancellationToken token = default);
    
    Task<RTWErrorCode> JoinRoomAsync(string roomId, IPlayer player);
    Task<RTWErrorCode> LeaveRoomAsync(string roomId, string sessionId);
    int CleanupSession(string sessionId);
    bool IsMember(string roomId, string sessionId);
}
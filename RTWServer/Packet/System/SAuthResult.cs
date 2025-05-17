using NetworkDefinition.ErrorCode;
using RTWServer.Enum;

namespace RTWServer.Packet.System;

/// <summary>
/// Server to client authentication result packet
/// 목적: 실시간 서버 인증 결과 통보
/// 데이터: RTWErrorCode ErrorCode, int PlayerId (인증 성공 시 할당/확인된 플레이어 고유 ID)
/// </summary>
public class SAuthResult : BasePacket
{
    public RTWErrorCode ErrorCode { get; }
    public int PlayerId { get; }

    public SAuthResult(PacketId packetId, byte[] payload) 
        : base(packetId, payload)
    {
        ErrorCode = (RTWErrorCode)BitConverter.ToInt32(payload, 0);
        PlayerId = BitConverter.ToInt32(payload, 4);
    }

    /// <summary>
    /// Creates a new SAuthResult packet with authentication success result
    /// </summary>
    /// <param name="playerId">The player ID assigned to the authenticated user</param>
    public SAuthResult(int playerId) 
        : base(PacketId.SAuthResult, CreateSuccessPayload(playerId))
    {
        ErrorCode = RTWErrorCode.Success;
        PlayerId = playerId;
    }

    /// <summary>
    /// Creates a new SAuthResult packet with authentication failure result
    /// </summary>
    /// <param name="errorCode">The error code for authentication failure</param>
    public SAuthResult(RTWErrorCode errorCode) 
        : base(PacketId.SAuthResult, CreateErrorPayload(errorCode))
    {
        ErrorCode = errorCode;
        PlayerId = 0;
    }

    private static byte[] CreateSuccessPayload(int playerId)
    {
        byte[] errorCodeBytes = BitConverter.GetBytes((int)RTWErrorCode.Success);
        byte[] playerIdBytes = BitConverter.GetBytes(playerId);

        byte[] payload = new byte[errorCodeBytes.Length + playerIdBytes.Length];
        
        Buffer.BlockCopy(errorCodeBytes, 0, payload, 0, errorCodeBytes.Length);
        Buffer.BlockCopy(playerIdBytes, 0, payload, errorCodeBytes.Length, playerIdBytes.Length);
        
        return payload;
    }

    private static byte[] CreateErrorPayload(RTWErrorCode errorCode)
    {
        byte[] errorCodeBytes = BitConverter.GetBytes((int)errorCode);
        byte[] playerIdBytes = BitConverter.GetBytes(0); // Zero player ID for failure

        byte[] payload = new byte[errorCodeBytes.Length + playerIdBytes.Length];
        
        Buffer.BlockCopy(errorCodeBytes, 0, payload, 0, errorCodeBytes.Length);
        Buffer.BlockCopy(playerIdBytes, 0, payload, errorCodeBytes.Length, playerIdBytes.Length);
        
        return payload;
    }
}

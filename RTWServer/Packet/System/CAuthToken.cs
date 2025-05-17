using System.Text;
using RTWServer.Enum;

namespace RTWServer.Packet.System;

/// <summary>
/// Client to server authentication token packet
/// 목적: 웹 서버 발급 토큰으로 실시간 서버 인증 요청
/// 데이터: string AuthToken
/// </summary>
public class CAuthToken : BasePacket
{
    public string AuthToken { get; }

    public CAuthToken(PacketId packetId, byte[] payload) 
        : base(packetId, payload)
    {
        AuthToken = Encoding.UTF8.GetString(Payload);
    }
    
    /// <summary>
    /// Creates a new CAuthToken packet with the specified auth token
    /// </summary>
    /// <param name="authToken">The authentication token from the web server</param>
    public CAuthToken(string authToken) 
        : base(PacketId.CAuthToken, Encoding.UTF8.GetBytes(authToken))
    {
        AuthToken = authToken;
    }
}

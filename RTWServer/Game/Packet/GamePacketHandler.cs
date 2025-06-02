using Microsoft.Extensions.Logging;
using NetworkDefinition.ErrorCode;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Packet;

public class GamePacketHandler(ILoggerFactory loggerFactory) : IPacketHandler
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GamePacketHandler>();

    public async Task HandlePacketAsync(IPacket packet, IClientSession clientSession)
    {
        // _logger.LogInformation($"PacketId: {packet.PacketId}");

        switch (packet.PacketId)
        {
            case PacketId.EchoMessage:
                await clientSession.SendAsync(packet);
                break;

            case PacketId.CAuthToken:
                if (packet.GetPayloadMessage() is CAuthToken authTokenProto)
                {
                    await HandleAuthToken(authTokenProto, clientSession);
                }
                else
                {
                    _logger.LogWarning("Could not cast payload to CAuthToken for packet ID: {PacketPacketId}", packet.PacketId);
                    // Optionally, send an error response or close the session
                }
                break;

            default:
                _logger.LogWarning($"Unknown packet ID: {packet.PacketId}");
                throw new ArgumentOutOfRangeException(nameof(packet.PacketId), packet.PacketId, null);
        }
    }

    private async Task HandleAuthToken(CAuthToken authTokenProtoPacket, IClientSession clientSession)
    {
        string authToken = authTokenProtoPacket.AuthToken;
        _logger.LogDebug("Received authentication token: {AuthToken} from client {ClientId}", 
            authToken, clientSession.Id);

        // Validate token using the method on ClientSession
        var (errorCode, playerId) = await clientSession.ValidateAuthTokenAsync(authToken);
        
        if (errorCode == RTWErrorCode.Success)
        {
            _logger.LogInformation("Authentication successful for client {ClientId}, PlayerId: {PlayerId}", 
                clientSession.Id, playerId);
            
            var sAuthResultProto = new SAuthResult 
            { 
                PlayerId = playerId, 
                ErrorCode = (int)RTWErrorCode.Success // Cast to int for proto
            };
            await clientSession.SendAsync(new ProtoPacket(PacketId.SAuthResult, sAuthResultProto));
        }
        else
        {
            _logger.LogWarning("Authentication failed for client {ClientId}, ErrorCode: {ErrorCode}", 
                clientSession.Id, errorCode);
            
            var sAuthResultProto = new SAuthResult 
            { 
                ErrorCode = (int)errorCode // Cast to int for proto
            };
            await clientSession.SendAsync(new ProtoPacket(PacketId.SAuthResult, sAuthResultProto));
        }
    }
}
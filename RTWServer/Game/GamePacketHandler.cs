using Microsoft.Extensions.Logging;
using NetworkDefinition.ErrorCode;
using RTWServer.Auth;
using RTWServer.Enum;
using RTWServer.Packet.System;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game;

public class GamePacketHandler : IPacketHandler
{
    private readonly ILogger _logger;
    private readonly IUserStateManager _userStateManager;

    public GamePacketHandler(ILoggerFactory loggerFactory, IUserStateManager userStateManager)
    {
        _logger = loggerFactory.CreateLogger<GamePacketHandler>();
        _userStateManager = userStateManager;
    }

    public async Task HandlePacketAsync(IPacket packet, IClientSession clientSession)
    {
        // _logger.LogInformation($"PacketId: {packet.PacketId}");

        switch (packet.PacketId)
        {
            case PacketId.EchoTest:
                await clientSession.SendAsync(packet);
                break;

            case PacketId.CAuthToken:
                await HandleAuthToken((CAuthToken)packet, clientSession);
                break;

            default:
                _logger.LogWarning($"Unknown packet ID: {packet.PacketId}");
                throw new ArgumentOutOfRangeException(nameof(packet.PacketId), packet.PacketId, null);
        }
    }

    private async Task HandleAuthToken(CAuthToken authTokenPacket, IClientSession clientSession)
    {
        string authToken = authTokenPacket.AuthToken;
        _logger.LogDebug("Received authentication token: {AuthToken} from client {ClientId}", 
            authToken, clientSession.Id);

        // Here you would validate the token against your authentication system
        var (errorCode, playerId) = await _userStateManager.ValidateAuthTokenAsync(authToken);
        
        if (errorCode == RTWErrorCode.Success)
        {
            _logger.LogInformation("Authentication successful for client {ClientId}, PlayerId: {PlayerId}", 
                clientSession.Id, playerId);
            
            await clientSession.SendAsync(new SAuthResult(playerId));
        }
        else
        {
            _logger.LogWarning("Authentication failed for client {ClientId}, ErrorCode: {ErrorCode}", 
                clientSession.Id, errorCode);
            
            await clientSession.SendAsync(new SAuthResult(errorCode));
        }
    }
}
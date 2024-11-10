using Microsoft.Extensions.Logging;
using RTWServer.ServerCore;

namespace RTWServer.Game;

public class GamePacketHandler : IPacketHandler
{
    private readonly ILogger _logger;

    public GamePacketHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GamePacketHandler>();
    }

    public async Task HandlePacketAsync(int packetId, byte[] payload, IClient client)
    {
        _logger.LogInformation($"PacketId: {packetId}, Payload: {payload.Length} bytes");

        switch ((PacketId)packetId)
        {
            case PacketId.EchoTest:
                await client.SendPacketAsync(PacketId.EchoTest, payload);
                break;

            default:
                _logger.LogWarning($"Unknown packet ID: {packetId}");
                throw new ArgumentOutOfRangeException(nameof(packetId), packetId, null);
        }
    }
}
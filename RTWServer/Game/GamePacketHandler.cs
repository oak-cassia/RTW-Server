using Microsoft.Extensions.Logging;
using RTWServer.Enum;
using RTWServer.ServerCore;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game;

public class GamePacketHandler : IPacketHandler
{
    private readonly ILogger _logger;

    public GamePacketHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GamePacketHandler>();
    }

    public async Task HandlePacketAsync(IPacket packet, IClient client)
    {
        // _logger.LogInformation($"PacketId: {packet.PacketId}");

        switch (packet.PacketId)
        {
            case PacketId.EchoTest:
                await client.SendAsync(PacketId.EchoTest, packet.Serialize());
                break;

            default:
                _logger.LogWarning($"Unknown packet ID: {packet.PacketId}");
                throw new ArgumentOutOfRangeException(nameof(packet.PacketId), packet.PacketId, null);
        }
    }
}
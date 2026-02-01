using Microsoft.Extensions.Logging;
using NetworkDefinition.ErrorCode;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Game.Chat;
using GamePlayer = RTWServer.Game.Player.Player;
using RTWServer.Packet;
using RTWServer.ServerCore.Interface;

namespace RTWServer.Game.Packet;

public class GamePacketHandler(ILoggerFactory loggerFactory, IChatService chatService, string defaultChatRoomId) : IPacketHandler
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GamePacketHandler>();
    private readonly IChatService _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));

    private readonly string _defaultChatRoomId = string.IsNullOrWhiteSpace(defaultChatRoomId)
        ? throw new ArgumentException("Default room id cannot be null or whitespace.", nameof(defaultChatRoomId))
        : defaultChatRoomId;

    public async Task HandlePacketAsync(IPacket packet, IClientSession clientSession)
    {
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
                    // 필요하다면 오류 응답을 보내거나 세션을 종료합니다.
                }

                break;

            case PacketId.CChat:
                if (packet.GetPayloadMessage() is CChat cChat)
                {
                    await HandleChat(cChat, clientSession);
                }
                else
                {
                    _logger.LogWarning("Could not cast payload to CChat for packet ID: {PacketPacketId}", packet.PacketId);
                }

                break;

            case PacketId.CChatChat:
                if (packet.GetPayloadMessage() is CChatChat cChatChat)
                {
                    await HandleChatChat(cChatChat, clientSession);
                }
                else
                {
                    _logger.LogWarning("Could not cast payload to CChatChat for packet ID: {PacketPacketId}", packet.PacketId);
                }

                break;

            case PacketId.CChatJoin:
                if (packet.GetPayloadMessage() is CChatJoin cChatJoin)
                {
                    await HandleChatJoin(cChatJoin, clientSession);
                }
                else
                {
                    _logger.LogWarning("Could not cast payload to CChatJoin for packet ID: {PacketPacketId}", packet.PacketId);
                }

                break;

            case PacketId.CChatLeave:
                if (packet.GetPayloadMessage() is CChatLeave cChatLeave)
                {
                    await HandleChatLeave(cChatLeave, clientSession);
                }
                else
                {
                    _logger.LogWarning("Could not cast payload to CChatLeave for packet ID: {PacketPacketId}", packet.PacketId);
                }

                break;

            case PacketId.ISessionClosed:
                HandleSessionClosed(clientSession);
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

        // ClientSession의 메서드를 통해 토큰을 검증합니다.
        var (errorCode, playerId) = await clientSession.ValidateAuthTokenAsync(authToken);

        if (errorCode == RTWErrorCode.Success)
        {
            _logger.LogInformation("Authentication successful for client {ClientId}, PlayerId: {PlayerId}",
                clientSession.Id, playerId);

            var sAuthResultProto = new SAuthResult
            {
                PlayerId = playerId,
                ErrorCode = (int)RTWErrorCode.Success // proto 전송을 위해 int로 캐스팅
            };
            await clientSession.SendAsync(new ProtoPacket(PacketId.SAuthResult, sAuthResultProto));
        }
        else
        {
            _logger.LogWarning("Authentication failed for client {ClientId}, ErrorCode: {ErrorCode}",
                clientSession.Id, errorCode);

            var sAuthResultProto = new SAuthResult
            {
                ErrorCode = (int)errorCode // proto 전송을 위해 int로 캐스팅
            };
            await clientSession.SendAsync(new ProtoPacket(PacketId.SAuthResult, sAuthResultProto));
        }
    }

    private async Task HandleChat(CChat cChat, IClientSession clientSession)
    {
        string message = cChat.Message ?? string.Empty;
        _logger.LogInformation("Chat received: client {ClientId} room {RoomId} type {ChatType} len {MessageLength}",
            clientSession.Id, _defaultChatRoomId, cChat.ChatType, message.Length);

        var result = await _chatService.SendChatMessageAsync(_defaultChatRoomId, clientSession.Id, clientSession.Id, message, cChat.ChatType)
            .ConfigureAwait(false);

        if (result != RTWErrorCode.Success)
        {
            _logger.LogWarning("Chat failed: client {ClientId} room {RoomId} error {ErrorCode}",
                clientSession.Id, _defaultChatRoomId, result);
        }
    }

    private async Task HandleChatChat(CChatChat cChatChat, IClientSession clientSession)
    {
        if (!clientSession.IsAuthenticated)
        {
            _logger.LogWarning("ChatChat rejected: client {ClientId} is not authenticated", clientSession.Id);
            return;
        }

        string message = cChatChat.Message ?? string.Empty;
        _logger.LogInformation("ChatChat received: client {ClientId} room {RoomId} len {MessageLength}",
            clientSession.Id, _defaultChatRoomId, message.Length);

        var result = await _chatService.SendChatMessageAsync(_defaultChatRoomId, clientSession.Id, clientSession.Id, message)
            .ConfigureAwait(false);

        if (result != RTWErrorCode.Success)
        {
            _logger.LogWarning("ChatChat failed: client {ClientId} room {RoomId} error {ErrorCode}",
                clientSession.Id, _defaultChatRoomId, result);
        }
    }

    private async Task HandleChatJoin(CChatJoin cChatJoin, IClientSession clientSession)
    {
        string roomId = cChatJoin.RoomId ?? string.Empty;
        if (!clientSession.IsAuthenticated)
        {
            await SendChatJoinResult(clientSession, roomId, RTWErrorCode.AuthenticationFailed).ConfigureAwait(false);
            return;
        }

        var player = new GamePlayer(clientSession.Id.GetHashCode(), clientSession.Id, clientSession.Id);
        var result = await _chatService.JoinRoomAsync(roomId, player).ConfigureAwait(false);
        await SendChatJoinResult(clientSession, roomId, result).ConfigureAwait(false);
    }

    private async Task HandleChatLeave(CChatLeave cChatLeave, IClientSession clientSession)
    {
        string roomId = cChatLeave.RoomId ?? string.Empty;
        var result = await _chatService.LeaveRoomAsync(roomId, clientSession.Id).ConfigureAwait(false);
        await SendChatLeaveResult(clientSession, roomId, result).ConfigureAwait(false);
    }

    private async Task SendChatJoinResult(IClientSession clientSession, string roomId, RTWErrorCode errorCode)
    {
        var result = new SChatJoinResult
        {
            ErrorCode = (int)errorCode,
            RoomId = roomId
        };

        await clientSession.SendAsync(new ProtoPacket(PacketId.SChatJoinResult, result)).ConfigureAwait(false);
    }

    private async Task SendChatLeaveResult(IClientSession clientSession, string roomId, RTWErrorCode errorCode)
    {
        var result = new SChatLeaveResult
        {
            ErrorCode = (int)errorCode,
            RoomId = roomId
        };

        await clientSession.SendAsync(new ProtoPacket(PacketId.SChatLeaveResult, result)).ConfigureAwait(false);
    }

    private void HandleSessionClosed(IClientSession clientSession)
    {
        _logger.LogInformation("Handling internal session closed for client {ClientId}", clientSession.Id);
        _chatService.CleanupSession(clientSession.Id);
    }
}
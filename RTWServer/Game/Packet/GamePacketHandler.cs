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
                if (!clientSession.IsAuthenticated)
                {
                    _logger.LogWarning("Echo rejected: client {ClientId} is not authenticated", clientSession.Id);
                    break;
                }

                await clientSession.SendAsync(packet);
                break;

            case PacketId.CAuthToken:
                if (packet.GetPayloadMessage() is CAuthToken authTokenProto)
                {
                    await HandleAuthToken(authTokenProto, clientSession);
                }
                else
                {
                    _logger.LogWarning("Invalid payload for CAuthToken from client {ClientId}", clientSession.Id);
                }

                break;

            case PacketId.CChat:
                if (packet.GetPayloadMessage() is CChat cChat)
                {
                    await HandleChat(cChat, clientSession);
                }
                else
                {
                    _logger.LogWarning("Invalid payload for CChat from client {ClientId}", clientSession.Id);
                }

                break;

            case PacketId.CChatChat:
                if (packet.GetPayloadMessage() is CChatChat cChatChat)
                {
                    await HandleChatChat(cChatChat, clientSession);
                }
                else
                {
                    _logger.LogWarning("Invalid payload for CChatChat from client {ClientId}", clientSession.Id);
                }

                break;

            case PacketId.CChatJoin:
                if (packet.GetPayloadMessage() is CChatJoin cChatJoin)
                {
                    await HandleChatJoin(cChatJoin, clientSession);
                }
                else
                {
                    _logger.LogWarning("Invalid payload for CChatJoin from client {ClientId}", clientSession.Id);
                }

                break;

            case PacketId.CChatLeave:
                if (packet.GetPayloadMessage() is CChatLeave cChatLeave)
                {
                    await HandleChatLeave(cChatLeave, clientSession);
                }
                else
                {
                    _logger.LogWarning("Invalid payload for CChatLeave from client {ClientId}", clientSession.Id);
                }

                break;

            case PacketId.ISessionClosed:
                HandleSessionClosed(clientSession);
                break;

            default:
                _logger.LogWarning("Unknown packet ID: {PacketId} from client {ClientId}. Requesting shutdown.", packet.PacketId, clientSession.Id);
                await clientSession.RequestShutdownAsync($"Unknown packet ID: {packet.PacketId}");
                break;
        }
    }

    private async Task HandleAuthToken(CAuthToken authTokenProtoPacket, IClientSession clientSession)
    {
        string authToken = authTokenProtoPacket.AuthToken;
        long userId = authTokenProtoPacket.UserId;
        _logger.LogDebug("Received authentication token from client {ClientId} for userId {UserId}", clientSession.Id, userId);

        RTWErrorCode errorCode = await clientSession.ValidateAuthTokenAsync(userId, authToken);

        var sAuthResultProto = new SAuthResult
        {
            PlayerId = (errorCode == RTWErrorCode.Success)
                ? clientSession.UserId
                : 0,
            ErrorCode = (int)errorCode
        };

        if (errorCode == RTWErrorCode.Success)
        {
            _logger.LogInformation("Authentication successful for client {ClientId}, PlayerId: {PlayerId}", clientSession.Id, clientSession.UserId);

            // CChat/CChatChat은 RoomId 없이 기본 방으로 라우팅되므로, 인증 직후 기본 방에
            // 자동 입장시켜 두지 않으면 멤버십 검사에 걸려 메시지가 조용히 버려진다.
            var player = new GamePlayer(clientSession.UserId, clientSession.Id, clientSession.Id);
            var joinResult = await _chatService.JoinRoomAsync(_defaultChatRoomId, player);
            if (joinResult != RTWErrorCode.Success)
            {
                _logger.LogDebug("Default room join skipped for client {ClientId}: {ErrorCode}", clientSession.Id, joinResult);
            }
        }
        else
        {
            _logger.LogWarning("Authentication failed for client {ClientId}, ErrorCode: {ErrorCode}", clientSession.Id, errorCode);
        }

        await clientSession.SendAsync(new ProtoPacket(PacketId.SAuthResult, sAuthResultProto));
    }

    private async Task HandleChat(CChat cChat, IClientSession clientSession)
    {
        if (!clientSession.IsAuthenticated)
        {
            _logger.LogWarning("Chat rejected: client {ClientId} is not authenticated", clientSession.Id);
            return;
        }

        string message = cChat.Message ?? string.Empty;
        var result = await _chatService.SendChatMessageAsync(_defaultChatRoomId, clientSession.Id, message, cChat.ChatType);

        if (result != RTWErrorCode.Success)
        {
            _logger.LogWarning("Chat failed: client {ClientId} error {ErrorCode}", clientSession.Id, result);
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
        var result = await _chatService.SendChatMessageAsync(_defaultChatRoomId, clientSession.Id, message);

        if (result != RTWErrorCode.Success)
        {
            _logger.LogWarning("ChatChat failed: client {ClientId} error {ErrorCode}", clientSession.Id, result);
        }
    }

    private async Task HandleChatJoin(CChatJoin cChatJoin, IClientSession clientSession)
    {
        string roomId = cChatJoin.RoomId ?? string.Empty;
        if (!clientSession.IsAuthenticated)
        {
            await SendChatJoinResult(clientSession, roomId, RTWErrorCode.AuthenticationFailed);
            return;
        }

        var player = new GamePlayer(clientSession.UserId, clientSession.Id, clientSession.Id);
        var result = await _chatService.JoinRoomAsync(roomId, player);
        await SendChatJoinResult(clientSession, roomId, result);
    }

    private async Task HandleChatLeave(CChatLeave cChatLeave, IClientSession clientSession)
    {
        string roomId = cChatLeave.RoomId ?? string.Empty;
        if (!clientSession.IsAuthenticated)
        {
            await SendChatLeaveResult(clientSession, roomId, RTWErrorCode.AuthenticationFailed);
            return;
        }

        var result = await _chatService.LeaveRoomAsync(roomId, clientSession.Id);
        await SendChatLeaveResult(clientSession, roomId, result);
    }

    private async Task SendChatJoinResult(IClientSession clientSession, string roomId, RTWErrorCode errorCode)
    {
        var result = new SChatJoinResult
        {
            ErrorCode = (int)errorCode,
            RoomId = roomId
        };

        await clientSession.SendAsync(new ProtoPacket(PacketId.SChatJoinResult, result));
    }

    private async Task SendChatLeaveResult(IClientSession clientSession, string roomId, RTWErrorCode errorCode)
    {
        var result = new SChatLeaveResult
        {
            ErrorCode = (int)errorCode,
            RoomId = roomId
        };

        await clientSession.SendAsync(new ProtoPacket(PacketId.SChatLeaveResult, result));
    }

    private void HandleSessionClosed(IClientSession clientSession)
    {
        _logger.LogInformation("Handling internal session closed for client {ClientId}", clientSession.Id);
        _chatService.CleanupSession(clientSession.Id);
    }
}
﻿using System.Net;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using RTWServer.Authentication;
using RTWServer.Game.Chat;
using RTWServer.Game.Packet;
using RTWServer.Packet;
using RTWServer.ServerCore.implementation;
using RTWServer.ServerCore.Interface;

// 할 일: 설정 파일에서 IP 주소와 포트 번호를 읽어오도록 수정
string ipAddress = "127.0.0.1";
int port = 5000;

// 웹 서버 세션(session_{userId})을 조회하기 위한 Redis 연결. 웹 서버와 동일한 인스턴스를 가리켜야 한다.
// (웹 서버 env: DatabaseConfiguration__Redis 와 동일한 규칙을 따른다)
string redisConnection = Environment.GetEnvironmentVariable("DatabaseConfiguration__Redis") ?? "127.0.0.1:6379";

// 로거 팩토리와 로거 생성
ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole()
        .SetMinimumLevel(LogLevel.Debug);
});
ILogger logger = loggerFactory.CreateLogger("Program");

try
{
    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);

    const string defaultChatRoomId = "global";
    const string defaultChatRoomName = "Global";
    IClientSessionManager? clientSessionManager = null;
    IChatRoomManager chatRoomManager = new ChatRoomManager(sessionId => clientSessionManager?.GetClientSession(sessionId));
    chatRoomManager.GetOrCreateRoom(defaultChatRoomId, defaultChatRoomName, isPersistent: true);
    IChatService chatService = new ChatService(chatRoomManager);

    GamePacketFactory packetFactory = new GamePacketFactory();
    // IPacketHandler와 IPacketSerializer 인스턴스 생성
    IPacketSerializer packetSerializer = new PacketSerializer(packetFactory);

    GamePacketHandler packetHandler = new GamePacketHandler(loggerFactory, chatService, defaultChatRoomId);

    // 웹 서버와 동일한 RedisCache(IDistributedCache) 구현으로 세션 저장 포맷(Redis hash)을 맞춘다
    using RedisCache distributedCache = new RedisCache(new RedisCacheOptions { Configuration = redisConnection });
    ISessionValidator sessionValidator = new RedisSessionValidator(distributedCache, loggerFactory.CreateLogger<RedisSessionValidator>());

    // ClientSessionManager 생성 시 IPacketHandler와 IPacketSerializer 전달
    // ChatService 참조를 제거했습니다 (이제 내부 패킷을 통해 정리 처리를 수행합니다)
    clientSessionManager = new ClientSessionManager(loggerFactory, packetHandler, packetSerializer, sessionValidator);

    AsyncAwaitServer server = new AsyncAwaitServer(
        new TcpServerListener(endpoint, loggerFactory),
        // AsyncAwaitServer 생성자에서 제거되었으므로 여기서도 전달하지 않음
        loggerFactory,
        clientSessionManager // 초기화된 clientSessionManager 전달
    );

    logger.LogInformation("Server running at {IpAddress}:{Port}", ipAddress, port);

    // 서버 실행
    using CancellationTokenSource cts = new CancellationTokenSource();
    Task serverTask = server.Start(cts.Token);

    while (true)
    {
        string? input = Console.ReadLine();

        // null은 stdin EOF(파이프 종료, 데몬/컨테이너 환경)를 의미한다.
        // 계속 루프를 돌면 busy-loop로 CPU를 소모하므로 종료 처리한다.
        if (input == null || input == "quit")
        {
            logger.LogInformation("Shutdown requested ({Reason}), stopping server...", input == null ? "stdin closed" : "quit command");
            cts.Cancel();
            break;
        }
    }

    // 서버 정상 종료 대기
    await serverTask;
}
catch (Exception ex)
{
    // 예외 처리
    logger.LogError(ex, "An error occurred while starting the server");
}
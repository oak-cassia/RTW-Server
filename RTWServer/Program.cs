using System.Net;
using Microsoft.Extensions.Logging;
using RTWServer.Game.Chat;
using RTWServer.Game.Packet;
using RTWServer.Packet;
using RTWServer.ServerCore.implementation;
using RTWServer.ServerCore.Interface;

// 할 일: 설정 파일에서 IP 주소와 포트 번호를 읽어오도록 수정
string ipAddress = "127.0.0.1";
int port = 5000;

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
    chatRoomManager.CreateRoom(defaultChatRoomId, defaultChatRoomName);
    IChatHandler chatHandler = new ChatHandler(chatRoomManager);

    GamePacketFactory packetFactory = new GamePacketFactory();
    // IPacketHandler와 IPacketSerializer 인스턴스 생성
    IPacketSerializer packetSerializer = new PacketSerializer(packetFactory);

    GamePacketHandler packetHandler = new GamePacketHandler(loggerFactory, chatHandler, defaultChatRoomId);

    // ClientSessionManager 생성 시 IPacketHandler와 IPacketSerializer 전달
    clientSessionManager = new ClientSessionManager(loggerFactory, packetHandler, packetSerializer, chatHandler);

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
        if (input == "quit")
        {
            logger.LogInformation("Shutdown command received, stopping server...");
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
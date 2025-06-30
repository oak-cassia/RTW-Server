using System.Net;
using Microsoft.Extensions.Logging;
using RTWServer.Game.Packet;
using RTWServer.Packet;
using RTWServer.ServerCore.implementation;
using RTWServer.ServerCore.Interface;

// TODO : 설정 파일에서 IP 주소와 포트 번호를 읽어와서 사용하도록 수정
string ipAddress = "127.0.0.1";
int port = 5000;

// Create logger factory and logger
ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole()
        .SetMinimumLevel(LogLevel.Debug);
});
ILogger logger = loggerFactory.CreateLogger("Program");

try
{
    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);

    GamePacketFactory packetFactory = new GamePacketFactory();
    // IPacketHandler와 IPacketSerializer 인스턴스 생성
    IPacketHandler packetHandler = new GamePacketHandler(loggerFactory, packetFactory);
    IPacketSerializer packetSerializer = new PacketSerializer(packetFactory);

    // ClientSessionManager 생성 시 IPacketHandler와 IPacketSerializer 전달
    IClientSessionManager clientSessionManager = new ClientSessionManager(loggerFactory, packetHandler, packetSerializer); 

    AsyncAwaitServer server = new AsyncAwaitServer(
        new TcpServerListener(endpoint, loggerFactory),
        // packetHandler와 packetSerializer는 AsyncAwaitServer 생성자에서 제거되었으므로 여기서도 제거
        loggerFactory,
        clientSessionManager // Pass the initialized clientSessionManager
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

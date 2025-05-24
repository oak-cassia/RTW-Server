using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RTWServer.Game;
using RTWServer.Packet;
using RTWServer.ServerCore.implementation;
using RTWServer.ServerCore.Interface;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

string ipAddress = configuration.GetValue("ServerSettings:IPAddress", "127.0.0.1");
int port = configuration.GetValue("ServerSettings:Port", 5000);

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
    IPacketHandler packetHandler = new GamePacketHandler(loggerFactory);
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

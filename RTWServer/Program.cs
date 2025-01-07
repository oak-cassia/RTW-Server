﻿using System.Net;
using Microsoft.Extensions.Logging;
using RTWServer.Game;
using RTWServer.ServerCore.implementation;

// TODO : 설정 파일에서 IP 주소와 포트 번호를 읽어와서 사용하도록 수정
string ipAddress = "127.0.0.1";
int port = 5000;

try
{
    var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);

    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole()
            .SetMinimumLevel(LogLevel.Debug);
    });

    var server = new AsyncAwaitServer(
        endpoint,
        new GamePacketHandler(loggerFactory),
        loggerFactory,
        new GameClientFactory(),
        new GamePacketFactory()
    );

    Console.WriteLine($"Server running at {ipAddress}:{port}");

    // 서버 실행
    using var cts = new CancellationTokenSource();
    var serverTask = server.Start(cts.Token);

    while (true)
    {
        var input = Console.ReadLine();
        if (input == "quit")
        {
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
    Console.WriteLine($"An error occurred while starting the server: {ex.Message}");
}
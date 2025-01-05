using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace RTWServer.ServerCore;

class AsyncAwaitServer
{
    private const int HEADER_SIZE = 8;
    private const int HEADER_PACKET_ID_OFFSET = 0;
    private const int HEADER_LENGTH_OFFSET = 4;
    private const int BACKLOG = 100;
    private const int BUFFER_SIZE = 4096;

    // 서버 상태를 기록하는 필드
    private int _acceptCount; // 수락된 연결 수
    private int _readCount; // 읽은 데이터 수
    private int _closeByInvalidStream; // 잘못된 스트림으로 종료된 수

    private readonly ServerListener _serverListener;
    private readonly IPacketHandler _packetHandler;
    private readonly ILogger _logger;

    private readonly IPacketFactory _packetFactory;
    private readonly ClientManager _clientManager;

    public AsyncAwaitServer(
        ServerListener serverListener,
        IPacketHandler packetHandler,
        ILoggerFactory loggerFactory,
        IPacketFactory packetFactory,
        ClientManager clientManager
    )
    {
        _serverListener = serverListener;
        _packetHandler = packetHandler;
        _logger = loggerFactory.CreateLogger<AsyncAwaitServer>();
        _packetFactory = packetFactory;
        _clientManager = clientManager;
    }

    public async Task Start(CancellationToken token)
    {
        _serverListener.Start(BACKLOG);
        _logger.LogInformation("Server started...");

        try
        {
            while (!token.IsCancellationRequested)
            {
                IClient client = await _serverListener.AcceptClientAsync(token);

                Interlocked.Increment(ref _acceptCount);

                _clientManager.AddClient(client);

                _ = HandleClient(client);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Accepting client failed.");
        }
        finally
        {
            _serverListener.Stop();
            _logger.LogInformation("Server stopped.");
        }
    }

    private async Task HandleClient(IClient client)
    {
        try
        {
            // TODO: 메모리 풀 사용하는 거 공부하고 적용해보기, 카피 최소화 하자
            var buffer = new byte[BUFFER_SIZE];

            // 클라이언트로부터 패킷을 계속 읽음
            while (true)
            {
                var stream = client.GetStream();
                var packet = await HandleNetworkStream(stream, buffer);

                await _packetHandler.HandlePacketAsync(packet, client);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling the client.");
        }
        finally
        {
            _clientManager.RemoveClient(client);
        }
    }

    private async Task<IPacket> HandleNetworkStream(NetworkStream stream, byte[] buffer)
    {
        if (!await Fill(stream, buffer, HEADER_SIZE, HEADER_PACKET_ID_OFFSET))
        {
            Interlocked.Increment(ref _closeByInvalidStream);
            throw new InvalidOperationException("Failed to read header.");
        }

        var packetLength = BitConverter.ToInt32(buffer, HEADER_LENGTH_OFFSET);

        if (packetLength <= HEADER_SIZE || packetLength > BUFFER_SIZE)
        {
            Interlocked.Increment(ref _closeByInvalidStream);
            throw new InvalidOperationException("Invalid packet length.");
        }

        int payloadSize = packetLength - HEADER_SIZE;

        if (!await Fill(stream, buffer, payloadSize, HEADER_SIZE))
        {
            Interlocked.Increment(ref _closeByInvalidStream);
            throw new InvalidOperationException("Failed to read payload.");
        }

        var packetId = BitConverter.ToInt32(buffer, HEADER_PACKET_ID_OFFSET);

        // TODO : Memory랑 Span, https://learn.microsoft.com/ko-kr/dotnet/standard/memory-and-spans/memory-t-usage-guidelines
        var payload = new ReadOnlyMemory<byte>(buffer, HEADER_SIZE, payloadSize);

        return _packetFactory.CreatePacket(packetId, payload);
    }

    private async Task<bool> Fill(NetworkStream stream, byte[] buffer, int rest, int offset)
    {
        // 요청된 크기가 버퍼 크기를 초과하면 false 반환
        if (rest > buffer.Length)
        {
            return false;
        }

        // 남은 데이터를 모두 읽을 때까지 반복
        while (rest > 0)
        {
            var length = await stream.ReadAsync(buffer, offset, rest);
            Interlocked.Increment(ref _readCount);

            if (length == 0)
            {
                return false;
            }

            rest -= length;
            offset += length;
        }

        return true;
    }

    public override string ToString()
    {
        return string.Format("accept({0}) invalid_stream({1}) read({2})",
            _acceptCount,
            _closeByInvalidStream,
            _readCount
        );
    }
}
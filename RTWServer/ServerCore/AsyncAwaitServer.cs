using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace RTWServer.ServerCore
{
    class AsyncAwaitServer
    {
        // 서버 상태를 기록하는 필드
        private int _acceptCount; // 수락된 연결 수
        private int _readCount; // 읽은 데이터 수
        private int _closeByInvalidStream; // 잘못된 스트림으로 종료된 수

        private readonly IPEndPoint _endPoint;
        private readonly IPacketHandler _packetHandler;
        private readonly ILogger _logger;
        private readonly IClientFactory _clientFactory;
        private readonly IPacketFactory _packetFactory;

        private const int HeaderSize = 8;
        private const int HeaderPacketIdOffset = 0;
        private const int HeaderLengthOffset = 4;
        private const int Backlog = 100;
        private const int BufferSize = 4096;

        public AsyncAwaitServer(IPEndPoint endpoint, IPacketHandler packetHandler, ILoggerFactory loggerFactory, IClientFactory clientFactory,
            IPacketFactory packetFactory)
        {
            _endPoint = endpoint;
            _packetHandler = packetHandler;
            _logger = loggerFactory.CreateLogger<AsyncAwaitServer>();
            _clientFactory = clientFactory;
            _packetFactory = packetFactory;
        }

        public async Task Start()
        {
            var listener = new TcpListener(_endPoint);
            listener.Start(Backlog);

            while (true)
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                SetSocketOption(tcpClient.Client);

                IClient client = _clientFactory.CreateClient(tcpClient);
                HandleTcpClient(client);

                Interlocked.Increment(ref _acceptCount);
            }

            // ReSharper disable once FunctionNeverReturns
        }

        private async void HandleTcpClient(IClient client)
        {
            try
            {
                var buffer = new byte[BufferSize]; // TODO: 버퍼 풀 사용으로 교체

                // 클라이언트로부터 패킷을 계속 읽음
                while (true)
                {
                    var stream = client.GetStream();
                    var packet = await HandleNetworkStream(stream, buffer).ConfigureAwait(false);

                    await _packetHandler.HandlePacketAsync(packet, client).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while handling the client.");
                client.Close();
            }
        }

        private async Task<IPacket> HandleNetworkStream(NetworkStream stream, byte[] buffer)
        {
            if (!await Fill(stream, buffer, HeaderSize, HeaderPacketIdOffset).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _closeByInvalidStream);
                throw new InvalidOperationException("Failed to read header.");
            }

            var packetLength = BitConverter.ToInt32(buffer, HeaderLengthOffset);

            if (packetLength <= HeaderSize || packetLength > BufferSize)
            {
                Interlocked.Increment(ref _closeByInvalidStream);
                throw new InvalidOperationException("Invalid packet length.");
            }

            int payloadSize = packetLength - HeaderSize;

            if (!await Fill(stream, buffer, payloadSize, HeaderSize).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _closeByInvalidStream);
                throw new InvalidOperationException("Failed to read payload.");
            }

            var packetId = BitConverter.ToInt32(buffer, HeaderPacketIdOffset);

            // TODO : Memory랑 Span, https://learn.microsoft.com/ko-kr/dotnet/standard/memory-and-spans/memory-t-usage-guidelines
            var payload = new ReadOnlyMemory<byte>(buffer, HeaderSize, payloadSize);

            return _packetFactory.CreatePacket(packetId, payload);
        }

        private async Task<bool> Fill(NetworkStream stream, byte[] buffer, int rest, int offset)
        {
            // 요청된 크기가 버퍼 크기를 초과하면 false 반환
            if (rest > buffer.Length)
            {
                return false;
            }

            while (rest > 0) // 남은 데이터를 모두 읽을 때까지 반복
            {
                var length = await stream.ReadAsync(buffer, offset, rest).ConfigureAwait(false);
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

        private void SetSocketOption(Socket sock)
        {
            sock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true); // Nagle 알고리즘 비활성화
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
}
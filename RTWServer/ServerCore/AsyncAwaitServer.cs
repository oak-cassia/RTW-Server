using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace RTWServer.ServerCore
{
    // AwaitServer 클래스: 비동기 소켓 서버를 구현한 클래스
    class AwaitServer
    {
        // 서버 상태를 기록하는 필드
        private int _acceptCount; // 수락된 연결 수
        private int _readCount; // 읽은 데이터 수
        private int _closeByInvalidStream; // 잘못된 스트림으로 종료된 수

        private readonly IPEndPoint _endPoint; // 서버가 수신 대기할 IP 엔드포인트
        private readonly IPacketHandler _packetHandler;
        private readonly ILogger _logger;
        private readonly IClientFactory _clientFactory;

        private const int HeaderSize = 8; // 헤더 크기 (길이를 나타냄)
        private const int Backlog = 100; // 대기열 크기
        private const int BufferSize = 4096; // 버퍼 크기

        // 생성자: IP 엔드포인트를 설정
        public AwaitServer(IPEndPoint endpoint, IPacketHandler packetHandler, ILoggerFactory loggerFactory, IClientFactory clientFactory)
        {
            _endPoint = endpoint;
            _packetHandler = packetHandler;
            _logger = loggerFactory.CreateLogger<AwaitServer>();
            _clientFactory = clientFactory;
        }

        // 서버를 실행하는 비동기 메서드
        public async Task Start()
        {
            // TcpListener 생성 및 시작
            var listener = new TcpListener(_endPoint);
            listener.Start(Backlog); // 대기열 크기를 설정

            // 클라이언트 연결을 계속 대기
            while (true)
            {
                // 클라이언트 연결 요청을 비동기적으로 수락
                TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                Interlocked.Increment(ref _acceptCount); // 연결 수를 증가
                HandleTcpClient(client); // 연결된 클라이언트 처리
            }

            // ReSharper disable once FunctionNeverReturns
        }

        // 클라이언트 요청을 처리하는 메서드
        private async void HandleTcpClient(TcpClient tcpClient)
        {
            // 클라이언트 소켓 옵션 설정
            SetSocketOption(tcpClient.Client);
            IClient client = _clientFactory.CreateClient(tcpClient);

            try
            {
                await using var stream = client.GetStream();
                await HandleNetworkStream(client).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while handling the client.");
                client.Close();
            }
        }

        // 네트워크 스트림을 처리하는 비동기 메서드
        private async Task HandleNetworkStream(IClient client)
        {
            var buffer = new byte[BufferSize]; // TODO: 버퍼 풀 사용으로 교체

            while (true)
            {
                // 스트림에서 headerSize만큼 데이터를 읽음
                if (!await Fill(client.GetStream(), buffer, HeaderSize, 0).ConfigureAwait(false))
                {
                    Interlocked.Increment(ref _closeByInvalidStream); // 잘못된 스트림 카운트 증가
                    throw new InvalidOperationException("Failed to read header.");
                }

                // TODO IPacketFactory? 사용
                var packetId = BitConverter.ToInt32(buffer, 0);
                var packetLength = BitConverter.ToInt32(buffer, 4);

                if (packetLength <= HeaderSize || packetLength > BufferSize)
                {
                    Interlocked.Increment(ref _closeByInvalidStream); // 잘못된 스트림 카운트 증가
                    throw new InvalidOperationException("Invalid packet length.");
                }

                int payloadSize = packetLength - HeaderSize;
                if (!await Fill(client.GetStream(), buffer, payloadSize, HeaderSize).ConfigureAwait(false))
                {
                    Interlocked.Increment(ref _closeByInvalidStream); // 잘못된 스트림 카운트 증가
                    throw new InvalidOperationException("Failed to read payload.");
                }

                var payload = new byte[payloadSize];
                Array.Copy(buffer, HeaderSize, payload, 0, payload.Length);
                await _packetHandler.HandlePacketAsync(packetId, payload, client).ConfigureAwait(false);
            }
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
                var length = await stream.ReadAsync(buffer, offset, rest).ConfigureAwait(false); // 데이터를 읽음
                Interlocked.Increment(ref _readCount); // 읽기 작업 수 증가

                if (length == 0) // 읽은 데이터가 없으면 false 반환
                {
                    return false;
                }

                rest -= length; // 남은 데이터 크기를 줄임
                offset += length; // 읽은 위치를 증가
            }

            return true; // 성공적으로 데이터를 모두 읽었음
        }

        // 소켓 옵션 설정 메서드
        private void SetSocketOption(Socket sock)
        {
            sock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true); // Nagle 알고리즘 비활성화
        }

        // 서버 상태를 문자열로 반환
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
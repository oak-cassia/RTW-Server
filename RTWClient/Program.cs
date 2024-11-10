using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeSocketClient
{
    class TcpClientApp
    {
        private readonly string _serverIp;
        private readonly int _serverPort;

        public TcpClientApp(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public async Task RunAsync()
        {
            using var client = new TcpClient();

            try
            {
                Console.WriteLine($"Connecting to server {_serverIp}:{_serverPort}...");
                await client.ConnectAsync(_serverIp, _serverPort);
                Console.WriteLine("Connected to server!");

                var stream = client.GetStream();
                _ = Task.Run(() => ReceiveMessagesAsync(stream)); // 메시지 수신 비동기 처리

                while (true)
                {
                    Console.Write("> ");
                    var message = Console.ReadLine();

                    if (string.IsNullOrEmpty(message)) continue;
                    if (message.ToLower() == "quit") break;

                    var payload = Encoding.UTF8.GetBytes(message);
                    var packet = CreatePacket(10001, payload); // EchoTest의 PacketId는 10001
                    await stream.WriteAsync(packet, 0, packet.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Disconnected from server.");
            }
        }

        private async Task ReceiveMessagesAsync(NetworkStream stream)
        {
            var buffer = new byte[4096];

            try
            {
                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // 서버가 연결을 종료한 경우

                    var message = Encoding.UTF8.GetString(buffer, 8, bytesRead - 8); // Header를 제외하고 메시지 읽기
                    Console.WriteLine($"\nServer Echo: {message}");
                    Console.Write("> "); // 입력 프롬프트 다시 표시
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving messages: {ex.Message}");
            }
        }

        private byte[] CreatePacket(int packetId, byte[] payload)
        {
            var header = new byte[8];
            var packetIdBytes = BitConverter.GetBytes(packetId);
            var lengthBytes = BitConverter.GetBytes(payload.Length + 8);

            Array.Copy(packetIdBytes, 0, header, 0, 4);
            Array.Copy(lengthBytes, 0, header, 4, 4);

            var packet = new byte[header.Length + payload.Length];
            Array.Copy(header, 0, packet, 0, header.Length);
            Array.Copy(payload, 0, packet, header.Length, payload.Length);

            return packet;
        }

        static async Task Main(string[] args)
        {
            var clientApp = new TcpClientApp("127.0.0.1", 5000);
            await clientApp.RunAsync();
        }
    }
}

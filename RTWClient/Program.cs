using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Game;
using RTWServer.Packet;
using RTWServer.ServerCore;

namespace RTWClient
{
    class ClientSimulator
    {
        private readonly string _serverIp;
        private readonly int _serverPort;
        private readonly GamePacketFactory _packetFactory;
        private readonly PacketSerializer _packetSerializer;

        public ClientSimulator(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
            _packetFactory = new GamePacketFactory();
            _packetSerializer = new PacketSerializer(_packetFactory);
        }

        public async Task RunAsync()
        {
            Console.WriteLine("=== RTW E2E Test Client ===");
            Console.WriteLine("End-to-End network testing for RTW Server");
            Console.WriteLine();

            Console.WriteLine("Select E2E test:");
            Console.WriteLine("1. Echo Message E2E Test");
            Console.WriteLine("2. Auth Token E2E Test"); 
            Console.WriteLine("3. Exit");
            Console.Write("Choice (1-3): ");
            
            var input = Console.ReadLine();
            switch (input)
            {
                case "1":
                    await TestEchoNetworkConnection();
                    break;
                case "2":
                    await TestAuthTokenNetworkConnection();
                    break;
                case "3":
                default:
                    Console.WriteLine("Exiting E2E test client.");
                    break;
            }
        }

        private async Task TestEchoNetworkConnection()
        {
            using var client = new TcpClient();

            try
            {
                Console.WriteLine($"🌐 Connecting to server {_serverIp}:{_serverPort}...");
                await client.ConnectAsync(_serverIp, _serverPort);
                Console.WriteLine("✅ Connected to server!");

                var stream = client.GetStream();
                _ = Task.Run(() => ReceiveMessagesAsync(stream));

                Console.WriteLine("🔄 Echo Message Test - Enter messages to send (type 'quit' to exit):");
                while (true)
                {
                    Console.Write("> ");
                    var message = Console.ReadLine();

                    if (string.IsNullOrEmpty(message)) continue;
                    if (message.ToLower() == "quit") break;

                    // EchoMessage로 패킷 생성
                    var echoMessage = new EchoMessage { Message = message };
                    var packet = new ProtoPacket(PacketId.EchoMessage, echoMessage);
                    
                    // 직렬화 후 전송
                    await SendPacketAsync(stream, packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("🔌 Disconnected from server.");
            }
        }

        private async Task TestAuthTokenNetworkConnection()
        {
            using var client = new TcpClient();

            try
            {
                Console.WriteLine($"🌐 Connecting to server {_serverIp}:{_serverPort}...");
                await client.ConnectAsync(_serverIp, _serverPort);
                Console.WriteLine("✅ Connected to server!");

                var stream = client.GetStream();
                _ = Task.Run(() => ReceiveAuthMessagesAsync(stream));

                Console.WriteLine("🔐 Auth Token Test - Enter auth tokens to test (type 'quit' to exit):");
                Console.WriteLine("💡 Try tokens like: 'valid-token-123', 'invalid-token', 'test-auth-456'");
                
                while (true)
                {
                    Console.Write("Auth Token> ");
                    var authToken = Console.ReadLine();

                    if (string.IsNullOrEmpty(authToken)) continue;
                    if (authToken.ToLower() == "quit") break;

                    // CAuthToken으로 패킷 생성
                    var authTokenMessage = new CAuthToken { AuthToken = authToken };
                    var packet = new ProtoPacket(PacketId.CAuthToken, authTokenMessage);
                    
                    Console.WriteLine($"📤 Sending auth token: {authToken}");
                    
                    // 직렬화 후 전송
                    await SendPacketAsync(stream, packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("🔌 Disconnected from server.");
            }
        }

        private async Task SendPacketAsync(NetworkStream stream, ProtoPacket packet)
        {
            var totalSize = _packetSerializer.GetHeaderSize() + packet.GetPayloadSize();
            var buffer = new byte[totalSize];
            _packetSerializer.SerializeToBuffer(packet, buffer);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private async Task ReceiveAuthMessagesAsync(NetworkStream stream)
        {
            var buffer = new byte[4096];

            try
            {
                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    try
                    {
                        // 패킷 역직렬화 시도
                        var receivedData = new byte[bytesRead];
                        Array.Copy(buffer, 0, receivedData, 0, bytesRead);
                        
                        var packet = _packetSerializer.Deserialize(receivedData);
                        
                        if (packet is ProtoPacket protoPacket)
                        {
                            switch (protoPacket.PacketId)
                            {
                                case PacketId.SAuthResult:
                                    if (protoPacket.GetPayloadMessage() is SAuthResult authResult)
                                    {
                                        Console.WriteLine($"\n🔐 Auth Result Received:");
                                        Console.WriteLine($"   Player ID: {authResult.PlayerId}");
                                        Console.WriteLine($"   Error Code: {authResult.ErrorCode}");
                                        
                                        if (authResult.ErrorCode == 0)
                                        {
                                            Console.WriteLine("   ✅ Authentication Success!");
                                        }
                                        else
                                        {
                                            Console.WriteLine("   ❌ Authentication Failed!");
                                        }
                                    }
                                    break;
                                    
                                case PacketId.EchoMessage:
                                    if (protoPacket.GetPayloadMessage() is EchoMessage echoMsg)
                                    {
                                        Console.WriteLine($"\n📨 Server Echo: {echoMsg.Message}");
                                    }
                                    break;
                                    
                                default:
                                    Console.WriteLine($"\n📦 Received packet: {packet.PacketId}");
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n❌ Error parsing received packet: {ex.Message}");
                    }
                    
                    Console.Write("Auth Token> ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error receiving messages: {ex.Message}");
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
                    if (bytesRead == 0) break;

                    try
                    {
                        // 패킷 역직렬화 시도
                        var receivedData = new byte[bytesRead];
                        Array.Copy(buffer, 0, receivedData, 0, bytesRead);
                        
                        var packet = _packetSerializer.Deserialize(receivedData);
                        
                        if (packet is ProtoPacket protoPacket && protoPacket.GetPayloadMessage() is EchoMessage echoMsg)
                        {
                            Console.WriteLine($"\n📨 Server Echo: {echoMsg.Message}");
                        }
                        else
                        {
                            Console.WriteLine($"\n📦 Received packet: {packet.PacketId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n❌ Error parsing received packet: {ex.Message}");
                    }
                    
                    Console.Write("> ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error receiving messages: {ex.Message}");
            }
        }

        static async Task Main(string[] args)
        {
            var simulator = new ClientSimulator("127.0.0.1", 5000);
            await simulator.RunAsync();
        }
    }
}

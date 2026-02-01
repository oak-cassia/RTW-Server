using System.Net.Sockets;
using RTW.NetworkDefinition.Proto.Packet;
using RTWServer.Game.Packet;
using RTWServer.Packet;

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
            Console.WriteLine("3. Chat Room E2E Test");
            Console.WriteLine("4. Exit");
            Console.Write("Choice (1-4): ");

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
                    await TestChatRoomNetworkConnection();
                    break;

                case "4":
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

                    // EchoMessage 패킷 생성
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

                    // CAuthToken 패킷 생성
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

        private async Task TestChatRoomNetworkConnection()
        {
            using var client = new TcpClient();

            try
            {
                Console.WriteLine($"🌐 Connecting to server {_serverIp}:{_serverPort}...");
                await client.ConnectAsync(_serverIp, _serverPort);
                Console.WriteLine("✅ Connected to server!");

                var stream = client.GetStream();
                _ = Task.Run(() => ReceiveChatMessagesAsync(stream));

                Console.WriteLine("💬 Chat Room Test - Commands:");
                Console.WriteLine("   /auth <token>");
                Console.WriteLine("   /join <roomId>");
                Console.WriteLine("   /leave <roomId>");
                Console.WriteLine("   /msg <message>");
                Console.WriteLine("   /quit");

                while (true)
                {
                    Console.Write("Chat> ");
                    var input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        continue;
                    }

                    if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (input.StartsWith("/join ", StringComparison.OrdinalIgnoreCase))
                    {
                        var roomId = input.Substring(6).Trim();
                        var join = new CChatJoin { RoomId = roomId };
                        await SendPacketAsync(stream, new ProtoPacket(PacketId.CChatJoin, join));
                        continue;
                    }

                    if (input.StartsWith("/auth ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = input.Substring(6).Trim();
                        var auth = new CAuthToken { AuthToken = token };
                        await SendPacketAsync(stream, new ProtoPacket(PacketId.CAuthToken, auth));
                        continue;
                    }

                    if (input.StartsWith("/leave ", StringComparison.OrdinalIgnoreCase))
                    {
                        var roomId = input.Substring(7).Trim();
                        var leave = new CChatLeave { RoomId = roomId };
                        await SendPacketAsync(stream, new ProtoPacket(PacketId.CChatLeave, leave));
                        continue;
                    }

                    if (input.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase))
                    {
                        var message = input.Substring(5);
                        var chat = new CChatChat { Message = message };
                        await SendPacketAsync(stream, new ProtoPacket(PacketId.CChatChat, chat));
                        continue;
                    }

                    Console.WriteLine("Unknown command. Use /auth, /join, /leave, /msg, /quit.");
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

        private async Task ReceiveChatMessagesAsync(NetworkStream stream)
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
                        var receivedData = new byte[bytesRead];
                        Array.Copy(buffer, 0, receivedData, 0, bytesRead);

                        var packet = _packetSerializer.Deserialize(receivedData);
                        if (packet is not ProtoPacket protoPacket)
                        {
                            Console.WriteLine($"\n📦 Received packet: {packet.PacketId}");
                            continue;
                        }

                        switch (protoPacket.PacketId)
                        {
                            case PacketId.SAuthResult:
                                if (protoPacket.GetPayloadMessage() is SAuthResult authResult)
                                {
                                    Console.WriteLine($"\n🔐 Auth Result: player={authResult.PlayerId}, code={authResult.ErrorCode}");
                                }

                                break;

                            case PacketId.SChatJoinResult:
                                if (protoPacket.GetPayloadMessage() is SChatJoinResult joinResult)
                                {
                                    Console.WriteLine($"\n✅ Join Result: room={joinResult.RoomId}, code={joinResult.ErrorCode}");
                                }

                                break;

                            case PacketId.SChatLeaveResult:
                                if (protoPacket.GetPayloadMessage() is SChatLeaveResult leaveResult)
                                {
                                    Console.WriteLine($"\n✅ Leave Result: room={leaveResult.RoomId}, code={leaveResult.ErrorCode}");
                                }

                                break;

                            case PacketId.SChat:
                                if (protoPacket.GetPayloadMessage() is SChat sChat)
                                {
                                    Console.WriteLine($"\n💬 [{sChat.SenderName}] {sChat.Message}");
                                }

                                break;

                            default:
                                Console.WriteLine($"\n📦 Received packet: {packet.PacketId}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n❌ Error parsing received packet: {ex.Message}");
                    }

                    Console.Write("Chat> ");
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
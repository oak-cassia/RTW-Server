# RTWServer

이 디렉터리는 실시간 게임 서버를 구현하는 **RTWServer** 프로젝트의 소스 코드를 포함합니다. 프로젝트는 .NET 9을 사용하며 TCP 기반의 비동기 서버 구조를 제공합니다.

## 주요 구성

- **Program.cs** – 서버 시작 지점으로, 로그 설정 후 `AsyncAwaitServer` 를 생성하여 실행합니다. 종료 명령을 입력하면 서버가 중단됩니다.
- **ServerCore** – 서버의 핵심 기능을 담당하는 네임스페이스입니다. 비동기 소켓 서버(`AsyncAwaitServer`), 세션 관리(`ClientSessionManager`), 개별 세션 처리(`ClientSession`) 등이 구현되어 있습니다.
- **Game** – 게임 로직에 특화된 패킷 처리기(`GamePacketHandler`)와 패킷 팩토리(`GamePacketFactory`)를 제공합니다.
- **Packet** – 패킷 직렬화/역직렬화(`PacketSerializer`)와 다양한 패킷 정의가 위치합니다.
- **Enum** – 패킷 식별을 위한 `PacketId` 열거형을 정의합니다.
- **Docs** – 패킷 설계와 처리 흐름에 대한 문서가 포함되어 있습니다.

## 실행 흐름

`Program.cs`에서 서버가 초기화되는 과정의 일부는 다음과 같습니다.

```csharp
IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
int port = 5000;
var server = new AsyncAwaitServer(
    new TcpServerListener(new IPEndPoint(ipAddress, port), loggerFactory),
    loggerFactory,
    clientSessionManager);
Task serverTask = server.Start(cts.Token);
```
【F:RTWServer/Program.cs†L8-L43】

`AsyncAwaitServer`는 클라이언트 연결을 수락하고 `ClientSessionManager`에 위임합니다.

```csharp
public async Task Start(CancellationToken token)
{
    _serverListener.Start(MAX_PENDING_CONNECTIONS);
    while (!token.IsCancellationRequested)
    {
        IClient client = await _serverListener.AcceptClientAsync(token);
        _ = HandleClient(client, token);
    }
}
```
【F:RTWServer/ServerCore/implementation/AsyncAwaitServer.cs†L31-L45】

각 세션은 `ClientSession`을 통해 관리되며 패킷 직렬화는 `PacketSerializer`에서 담당합니다.

```csharp
public void SerializeToBuffer(IPacket packet, Span<byte> buffer)
{
    int payloadSize = packet.GetPayloadSize();
    BitConverter.TryWriteBytes(buffer, (int)packet.PacketId);
    BitConverter.TryWriteBytes(buffer.Slice(4), payloadSize);
    packet.WriteToBuffer(buffer.Slice(8, payloadSize));
}
```
【F:RTWServer/Packet/PacketSerializer.cs†L21-L29】

## 빌드 및 테스트

상위 솔루션 파일 `RTWServer.sln`을 통해 전체 프로젝트를 빌드할 수 있습니다. 테스트 프로젝트(`RTWTest`)도 포함되어 있으므로 `.NET` SDK가 설치된 환경에서 다음 명령으로 실행할 수 있습니다.

```bash
dotnet test
```

실행 환경에 .NET 9 SDK가 필요합니다.


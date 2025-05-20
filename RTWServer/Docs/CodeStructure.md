# RTWServer 코드 구조

이 문서는 **RTWServer** 프로젝트의 주요 폴더와 파일 구성을 설명합니다. 실시간 게임 서버를 담당하는 이 프로젝트는 다음과 같은 디렉터리로 구성되어 있습니다.

## 최상위 구조

```
RTWServer/
├── Docs/
├── Enum/
├── Game/
├── Packet/
├── Program.cs
├── RTWServer.csproj
└── ServerCore/
```

각 폴더의 역할은 다음과 같습니다.

### Docs
프로젝트 관련 문서를 보관합니다. 예시로 `Packet.md`가 있으며, 패킷 정의 및 처리 흐름을 정리해둡니다.

### Enum
공용 열거형을 정의합니다. 현재 `PacketId.cs`가 포함되어 있으며 패킷 식별자 목록을 제공합니다.

### Game
게임 로직과 관련된 패킷 생성 및 핸들러가 위치합니다.
- **GamePacketFactory**: 패킷 ID에 따라 구체적인 패킷 객체를 생성합니다.
- **GamePacketHandler**: 클라이언트로부터 수신한 패킷을 처리하고 필요한 응답을 전송합니다.

### Packet
패킷 직렬화/역직렬화와 각종 패킷 클래스가 들어 있습니다.
- **BasePacket** 및 **EchoPacket** 등 일반 패킷 구현
- **System** 하위 폴더에 인증 등 시스템 관련 패킷(`CAuthToken`, `SAuthResult`)이 위치
- **PacketSerializer**: 바이트 버퍼를 실제 패킷 객체로 변환하거나 반대로 변환합니다.

### ServerCore
서버 동작에 필요한 핵심 인터페이스와 구현체를 제공합니다.
- **Interface**: `IClient`, `IPacket`, `IServerListener` 등 서버 핵심 기능을 추상화한 인터페이스 모음
- **implementation**: TCP 서버 리스너(`TcpServerListener`), 클라이언트 세션 관리(`ClientSessionManager`), 세션 처리(`ClientSession`), 비동기 서버 실행부(`AsyncAwaitServer`) 등이 포함됩니다.

### Program.cs
서버 실행 진입점입니다. TCP 리스너 설정, 패킷 핸들러/팩토리 초기화, `AsyncAwaitServer` 구동 등을 담당합니다.

### RTWServer.csproj
.NET 프로젝트 파일로, 의존성 패키지와 참조 프로젝트(`NetworkDefinition`)를 명시합니다.

## 기타
- 실시간 서버는 TCP 기반으로 동작하며, `Program.cs`에서 `AsyncAwaitServer`를 시작하여 클라이언트의 연결을 처리합니다.
- 패킷 처리 흐름에 대한 상세 내용은 `Docs/Packet.md` 파일을 참고하십시오.


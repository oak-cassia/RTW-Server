# RTW-Server

> Real-Time and Web Server - 실시간 게임 서버 및 웹 API 서버

## 프로젝트 목표

- 기존의 라이브 서비스 경험을 넘어, 초기 개발 단계 학습
- 기능 확장, 교체가 용이한 아키텍처를 설계하며 OOP 체득
- C#과 .NET 환경에서 비동기 처리 및 서버 개발 역량 강화
- Code Agent를 적극 활용하여 AI 사용 능력 증진

## 프로젝트 개요

- 게임을 위한 실시간 서버와 웹 서버를 개발하는 것을 목표
- C#과 .NET 9을 사용하여 모노 리포지토리로 관리
- 실시간 소켓 통신을 위한 클라이언트 프로젝트와 유닛 테스트도 포함
- [Wiki](https://github.com/oak-cassia/RTW-Server/wiki)에서 더 자세한 설계 문서 확인 가능

## 프로젝트 구조

```
RTW-Server/
├── NetworkDefinition/          # 공통 네트워크 정의
│   ├── ErrorCode/             # 에러 코드 정의
│   └── Proto/                 # Protocol Buffer 정의
├── RTWServer/                 # 실시간 게임 서버
│   ├── ServerCore/            # 서버 핵심 로직 (비동기 소켓, 세션 관리)
│   ├── Game/                  # 게임 로직 및 패킷 처리
│   └── Packet/                # 패킷 직렬화/역직렬화
├── RTWWebServer/              # 웹 API 서버
│   ├── Controllers/           # REST API 컨트롤러
│   ├── Database/              # 데이터베이스 관련
│   └── Authentication/        # 커스텀 토큰 인증
├── RTWClient/                 # 테스트용 클라이언트
├── RTWTest/                   # 유닛 테스트
└── README.md
```

### 각 프로젝트 역할

- **NetworkDefinition**: 통신에 사용되는 공통 데이터 구조 정의(패킷 ID, Protocol Buffer 스키마, 에러 코드)
- **RTWServer**: TCP 소켓 기반의 실시간 게임 서버
- **RTWWebServer**: ASP.NET Core 기반의 웹 API 서버
- **RTWClient**: 실시간 서버 기능 테스트 및 디버깅을 위한 콘솔 기반 클라이언트
- **RTWTest**: 실시간 및 웹 서버의 유닛 테스트

## 사용 기술

- **언어**: C# 13
- **프레임워크**: .NET 9
- **실시간 통신**: TCP 소켓, Google Protocol Buffers
- **웹 서버**: ASP.NET Core
- **인증**: 커스텀 토큰 기반 인증
- **데이터베이스**: Entity Framework Core
- **테스트**: NUnit

## 개발 환경

- **IDE**: JetBrains Rider
- **런타임**: .NET 9.0
- **OS**: macOS
- **RDB**: MySQL
- **Cache**: Redis

## 기능 개요

### 🎮 실시간 서버 (RTWServer)

C# .NET 환경에서 비동기 소켓 통신을 기반으로 하는 TCP 게임 서버입니다. Protocol Buffers를 이용한 데이터 직렬화와 파이프라인 기반의 I/O 처리를 통해 실시간 게임 서버를 쉽게 구축할 수 있는 기반을 제공합니다.

![Type Dependencies Diagram for GamePacketFactory and other elements](https://github.com/user-attachments/assets/9c010a40-339b-4ad0-8e1d-453437c08798)

#### 주요 기능
- **비동기 TCP 소켓 통신**: async/await 패턴을 통한 비동기 처리
- **Protocol Buffers 기반 직렬화**: 효율적인 바이너리 데이터 통신
- **파이프라인 I/O**: .NET의 System.IO.Pipelines를 활용한 최적화된 데이터 처리
- **세션 관리**: 클라이언트별 독립적인 세션 관리 및 인증 처리
- **확장 가능한 패킷 시스템**: Factory 패턴을 통한 새로운 패킷 타입 쉬운 추가

#### 아키텍처 특징
- **의존성 주입**: 모든 핵심 컴포넌트가 인터페이스 기반으로 설계
- **계층형 구조**: ServerCore → Game → Packet 계층으로 책임 분리
- **스레드 안전성**: ConcurrentDictionary와 Lock을 활용한 안전한 동시성 처리

**🔗 자세한 내용**: [실시간 게임 서버 Wiki](https://github.com/oak-cassia/RTW-Server/wiki/%EC%8B%A4%EC%8B%9C%EA%B0%84-%EA%B2%8C%EC%9E%84-%EC%84%9C%EB%B2%84)

### 🌐 웹 서버 (RTWWebServer)

ASP.NET Core를 기반으로 하는 RESTful API 서버입니다. 게임 클라이언트의 인증, 계정 관리, 비 실시간 요소를 담당합니다.

#### 주요 기능
- **RESTful API**: 표준 HTTP 메서드를 통한 리소스 관리
- **커스텀 토큰 인증**: JWT 기반이 아닌 자체 토큰 시스템 구현
- **Entity Framework Core**: UnitOfWork 패턴을 통한 트랜잭션 및 데이터베이스 관리
- **계정 관리**: 회원가입, 로그인
- **데이터 캐싱**: Redis 활용 

#### 아키텍처 특징
- **Controller-Service-Repository 패턴**: 계층별 책임 분리
- **의존성 주입**: ASP.NET Core DI 컨테이너 활용
- **중앙화된 예외 처리**: 일관된 에러 응답 및 로깅

**🔗 자세한 내용**: [웹 API 서버 Wiki](https://github.com/oak-cassia/RTW-Server/wiki)

# RTW 잔여 작업 묶음·순서 계획 (2026-06-13)

세 차례 리뷰([2026-06-11], [2026-06-12], [2026-06-13])에서 도출된 미해결 항목과, 합의된
캐시·트랜잭션·세션 정합성 리팩터(P1~P4)를 **실행 가능한 묶음**으로 묶고 순서를 정한다.

P1(가챠 재화 원자적 차감)은 `feat/atomic-currency-deduction`에서 **완료**됐다. 이 문서는
나머지를 다룬다. 각 묶음은 "포함 항목 → 근거 → 의존성 → 변경 범위 → 선결정"을 기술한다.

> 기준: 새 인프라 결정이 없는 작고 확실한 수정을 앞에, 인프라·구성 결정이 따르는 작업을 뒤에.
> 진행 중인 정합성 트랙(P 계열)을 먼저 마무리하고, 게임서버 트랙을 그다음에 둔다.

---

## 권장 실행 순서 (한눈에)

| 순서 | 묶음 | 포함 | 트랙 | 변경 범위 | 새 인프라 결정 |
|---|---|---|---|---|---|
| 0 | 기록 정정 | proto 블로커 문구 수정 | 문서 | 사소 | 없음 |
| 1 | 웹서버 트랜잭션·닉네임 | P3(=M1) + M5 | 정합성 | 작음 | 없음 |
| 2 | 캐시 정합성 | P2(=M4) + P4(통합테스트) | 정합성 | 중간 | 테스트 인프라 |
| 3 | 게임서버 신뢰경계 | M3 + L2 | 게임서버 | 작음 | 없음 |
| 4 | 게임서버 인증 완성 | proto userId + 게임서버 Redis + 검증 연결 | 게임서버 | 중간 | Redis 연결·DI |
| 5 | 인증 rate limiting | M2 | 보안 | 중간 | rate limiter·프록시 |
| 6 | 정리·운영 | L1 / L3 / L4 / L5 | 위생 | 작음~중간 | 일부(해시 포맷) |

---

## 묶음 0 — 기록 정정 (즉시, 사소)

**포함**: 이전 docs/메모리의 *"`.proto` 원본이 리포에 없어 막혀 있다"*([2026-06-13-review-findings.md:191](2026-06-13-review-findings.md)) 문구를 사실에 맞게 수정.

**근거**: proto는 리포에 있고 빌드 시 자동 생성된다.
- 원본: [packet.proto](../NetworkDefinition/Proto/Source/packet.proto)
- Grpc.Tools가 [Packet.cs](../NetworkDefinition/Proto/Generated/Packet.cs)로 생성 ([NetworkDefinition.csproj:19](../NetworkDefinition/NetworkDefinition.csproj))
- 실제 블로커는 "proto 부재"가 아니라 **(1) `CAuthToken`에 userId 필드 없음, (2) 게임서버에 Redis 접근 수단 자체가 없음**. → 묶음 4에서 해소.

**의존성**: 없음. **변경 범위**: 문서 한 줄.

---

## 묶음 1 — 웹서버 트랜잭션·닉네임 (P3 + M5)

같은 파일(`GameEntryService.cs`)을 건드리므로 한 묶음.

### P3 (= M1) 세션 생성을 커밋 밖으로
- **위치**: [GameEntryService.cs:30-58](../RTWWebServer/Services/GameEntryService.cs)
- **문제**: `CommitAsync()` 이후의 Redis 작업(`CreateSessionAsync`)이 같은 try 안에 있어, Redis 장애 시 이미 커밋된 트랜잭션에 `RollbackAsync` 호출 → 원래 예외가 가려지고 원인 불명 500.
- **수정**: 세션 생성을 트랜잭션 try/catch **밖으로** 이동. 트랜잭션은 DB 작업만 감싼다.

### M5 기본 닉네임 `User_{accountId}` 선점 차단
- **위치**: [UserService.cs:90](../RTWWebServer/Services/UserService.cs), [GameEntryService.cs:66](../RTWWebServer/Services/GameEntryService.cs)
- **문제**: 임의 유저가 닉네임을 `User_123`으로 선점하면, account 123의 최초 입장이 유니크 제약 위반으로 **영구 실패**(타깃 DoS).
- **수정**: `UpdateNicknameAsync`의 `ValidateNickname`에서 `^User_\d+$` 예약 패턴을 거부(`ReservedNicknamePattern`)해 **원천 차단**. 이로써 `User_{accountId}`의 전역 유일성이 구조적으로 보장된다(accountId 유일 + 타 계정 선점 불가).
- **결정**: 유저 생성 catch에 대체 닉네임 재시도는 두지 **않는다**. 예약 검증이 닉네임 충돌을 불가능하게 만들므로 재시도 분기는 도달 불가능한 dead code가 된다. catch는 `uk_account_id` 경합(락 TTL 만료/failover 시 동일 account 동시 생성)만 단일 재조회로 처리한다. 레거시 데이터(예약 검증 도입 전 생성된 `User_\d+` 닉네임)는 **미고려** — 필요 시 데이터 마이그레이션으로 정리한다.

**의존성**: 없음(독립). **변경 범위**: 작음. **선결정**: 없음.

---

## 묶음 2 — 캐시 정합성 (P2 + P4)

### P2 캐시 레이어 교체 (= M4 닫음)
- **위치**: [CharacterGachaService.cs:96-127](../RTWWebServer/Services/CharacterGachaService.cs), [CacheManager.cs](../RTWWebServer/Cache/CacheManager.cs)
- **문제**: 읽기(GET `/Character/owned`)가 락을 건너뛰어, 가챠 무효화와 인터리빙되면 stale 목록이 기본 TTL **24시간** 잔존. `ICacheManager.Set`에 TTL 파라미터가 없어 단축 불가.
- **수정**: `CacheManager`/`RequestScopedLocalCache` 계열 제거 → `IDistributedCacheAdapter` 위에 얇은 `IPlayerCharacterCache`(cache-aside + 짧은 TTL).
- **주의**: 짧은 TTL은 read-after-invalidate 경합(락 없는 GET이 무효화 직후 stale을 다시 캐시에 씀)을 **제거가 아니라 시간 제한**할 뿐이다. 완전 정합은 락 내 read-through나 캐시 버저닝이 필요 — 여기서는 staleness 창 단축으로 충분하다고 보고 채택.
- **기각**: HybridCache(요청수명 AddScoped라 L1 이득 0, backplane 없어 정합성 약화).

### P4 원자 차감 통합테스트 (P1/P2 회귀 가드)
- **문제**: P1의 `ExecuteUpdateAsync`(조건부 UPDATE)는 **EF InMemory 미지원** → 차감 원자성·트래커 stale을 단위테스트로 못 잡음. 현재는 `Ignore(TransactionIgnoredWarning)`로 no-op 처리 중.
- **수정**: Testcontainers MySQL 또는 relational SQLite로 **통합테스트 최소 1개**. P2 리팩터의 안전망 역할도 하므로 P2와 같은 묶음.

**의존성**: P1(완료) 위. P2와 P4는 함께 가는 게 안전(P4가 P2 회귀를 잡음).
**변경 범위**: 중간~큼(DI 등록 + 모든 `ICacheManager` 소비자 + commit/rollback 머신 제거). **선결정**: 통합테스트 방식(Testcontainers vs SQLite).
> P2가 `CacheManager`를 통째로 제거하면 묶음6 L3의 "`CacheManager.RollbackAllChanges` 제거"는 **여기서 흡수**된다. L3에서 중복 처리하지 않는다.

---

## 묶음 3 — 게임서버 신뢰경계 (M3 + L2)

게임서버(`RTWServer`) 패킷 처리, proto·Redis **불필요**, 작고 확실. 묶음 4(인프라 동반) 전에 처리.

### M3 내부 전용 패킷 화이트리스트
- **위치**: [GamePacketFactory.cs:10-29](../RTWServer/Game/Packet/GamePacketFactory.cs)
- **문제**: 와이어 역직렬화가 모든 PacketId 허용 → 클라가 내부 전용 `ISessionClosed`(`I_SESSION_CLOSED=90001`)나 `S`-접두 패킷 주입 가능(신뢰 경계 위반).
- **수정**: 역직렬화 팩토리가 **클라 발신 집합만** 화이트리스트(접두사 아닌 명시 열거: `CAuthToken`, `CChat`/`CChatJoin`/`CChatLeave`/`CChatChat`, `EchoMessage`). `EchoMessage`(9999)는 C-접두가 아니지만 정당한 클라 패킷이라 접두사 기준은 누수 → 집합으로 정의. 허용 외(`S`-접두, `ISessionClosed`) PacketId는 세션 종료.

### L2 인증 게이트 누락 패킷
- **위치**: [GamePacketHandler.cs](../RTWServer/Game/Packet/GamePacketHandler.cs) — `EchoMessage`(미인증도 무제한 호출), `CChatLeave`(다른 채팅 핸들러와 달리 `IsAuthenticated` 검사 없음).
- **수정**: 인증 게이트 일관 적용.

**의존성**: 없음. **변경 범위**: 작음. **선결정**: 없음.

---

## 묶음 4 — 게임서버 인증 완성 (스텁 제거)

현재 [ValidateAuthTokenAsync](../RTWServer/ServerCore/implementation/ClientSession.cs)는
비어있지 않은 토큰을 전부 통과시키는 스텁([ClientSession.cs:366-386](../RTWServer/ServerCore/implementation/ClientSession.cs)).

### 실제 블로커 2개와 해소
1. **`CAuthToken`에 userId 없음** — 웹서버 세션은 `session_{userId}` 키([RemoteCacheKeyGenerator.cs:17](../RTWWebServer/Cache/RemoteCacheKeyGenerator.cs))로 저장, 검증은 `IsValidSessionAsync(userId, token)`([UserSessionProvider.cs:53](../RTWWebServer/Providers/Authentication/UserSessionProvider.cs))이라 **userId 필수**. 게임서버는 토큰만 받음.
   → **방안 A (proto 변경)**: proto [CAuthToken](../NetworkDefinition/Proto/Source/packet.proto)에 `int64 userId = 2;` 추가(빌드 시 자동 재생성), 클라가 토큰과 함께 userId 전송. userId 위조는 토큰이 비밀이라 무력하지만, 클라-신뢰 결합과 proto·클라 동시 변경이 따른다.
   → **방안 B (역방향 키, proto·클라 무변경)**: 웹서버가 세션 생성 시 역방향 키 `authtoken:{token} → userId`를 추가로 써 두면([UserSessionProvider.CreateSessionAsync](../RTWWebServer/Providers/Authentication/UserSessionProvider.cs)), 게임서버는 **토큰만으로** userId를 역조회해 검증 가능. proto/클라 변경 불필요, 클라가 userId를 보내지 않음(클라-신뢰 결합 제거). 단 키 2개의 TTL·정리 일관성 필요.
2. **게임서버에 Redis 접근 수단 0개** — `RTWServer` 전체에 StackExchange.Redis / `IDistributedCache` 참조 없음.
   → 게임서버에 Redis 클라이언트 추가, 웹서버와 **동일한 `session_{userId}` 키 포맷** + `UserSession` 역직렬화 + 상수시간 토큰 비교.

### 연결
- `ValidateAuthTokenAsync`를 위 검증에 연결, 스텁 제거.
- **PlayerId↔userId 매핑**: 현재 `ValidateAuthTokenAsync`는 토큰과 무관한 `PlayerId`를 반환([ClientSession.cs:380](../RTWServer/ServerCore/implementation/ClientSession.cs)). 검증 후엔 **검증된 userId를 PlayerId로 채택**해, 채팅 등 이후 흐름이 신뢰된 식별자를 쓰도록 한다.

**의존성**: 묶음 3 권장(같은 게임서버 영역, 작은 것부터). **변경 범위**: 중간.
**선결정**: ⚠️ 방안 A(proto+클라 userId) vs 방안 B(역방향 키, proto·클라 무변경) 중 택1, 게임서버 Redis 연결 문자열·DI·구성, 키 포맷 공유 방식(상수/공용 라이브러리).

---

## 묶음 5 — 인증 rate limiting (M2)

- **위치**: [LoginController.cs](../RTWWebServer/Controllers/LoginController.cs), [AccountController.cs](../RTWWebServer/Controllers/AccountController.cs)
- **문제**: `/Login/login`, `/Account/create*`에 속도 제한 전무 → 무차별 대입·크리덴셜 스터핑·게스트 계정 무한 생성. **남은 가장 큰 보안 공백.**
- **순서 주의**: 위험도상 가장 큰 공백이지만, 프록시/`X-Forwarded-For` 신뢰 경계 결정에 의존하므로 "작은 것·인프라 무관 먼저" 원칙에 따라 의도적으로 #5에 둔다.
- **수정**: `AddRateLimiter` + `UseRateLimiter`로 IP/이메일 기준 제한. 프록시 뒤라면 `ForwardedHeaders`(`X-Forwarded-For`) 신뢰 설정 동반.

**의존성**: 없음. **변경 범위**: 중간. **선결정**: ⚠️ 제한 정책, 프록시/`X-Forwarded-For` 신뢰 경계.

---

## 묶음 6 — 정리·운영 (L급)

| 항목 | 내용 | 위치 |
|---|---|---|
| L1 | finally의 `UnlockAsync` Redis 실패가 응답을 깨뜨림 → try/catch + 경고 로그 | [RequestLockingMiddleware.cs:41-47](../RTWWebServer/Middlewares/RequestLockingMiddleware.cs) |
| L3 | 죽은 코드 제거(다수 repository 메서드, `JwtTokenProvider.ValidateJwt/ParseJwtToken`, `CacheManager.RollbackAllChanges` 등) | 여러 곳 |
| L4 | PBKDF2 반복(100k→권장 600k) + **버전 있는 자기서술 해시 포맷**(`{버전}.{반복}.{salt}.{hash}`)으로 점진적 재해싱 경로 확보 | [PasswordHasher.cs](../RTWWebServer/Providers/Authentication/PasswordHasher.cs) |
| L5 | `AsNoTracking` 분리 적용 / `RequestAborted` 토큰 전파 / 헬스체크 `/health` / 프로덕션 로그레벨 분리 | 여러 곳 |

**선결정**: L4 해시 포맷은 향후 마이그레이션 영향이 있어 별도 검토. 나머지는 독립적.

---

## 트랙 분리 요약

- **정합성 트랙(진행 중)**: P1✓ → 묶음1(P3·M5) → 묶음2(P2·P4)
- **게임서버 트랙**: 묶음3(M3·L2) → 묶음4(인증 완성)
- **보안/위생**: 묶음5(M2) → 묶음6(L1·L3·L4·L5)

묶음 1·3은 작고 독립적이라 언제든 먼저 처리 가능. 묶음 2·4·5는 인프라/구성 결정을 동반하므로
각각 별도 작업으로 분리한다.

---

## 이전부터 알려진 미해결 (이 계획 범위 밖)

- 마스터데이터 런타임 리로드 시 검증 예외가 OptionsMonitor 내부(콜백 이전)에서 발생하는 경로.
- 가챠 `count` 상한 부재·`gachaType` 미사용.
- git 히스토리의 옛 시크릿 로테이션.
- 멱등성(이중 처리): P1은 음수 잔액(더블스펜드)은 막지만 재시도 이중 처리는 못 막음 — 진짜 exactly-once는 idempotency key 필요(범위 밖).

[2026-06-11]: ./2026-06-webserver-improvement-report.md
[2026-06-12]: ./2026-06-12-gameserver-webserver-improvement-report.md
[2026-06-13]: ./2026-06-13-review-findings.md

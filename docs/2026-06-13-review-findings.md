# RTW 게임 서버 · 웹서버 리뷰 발견사항 (2026-06-13)

전체 코드베이스 3차 리뷰. 이전 두 차례(2026-06-11, 2026-06-12) 수정에서 다루지 않은
항목만 정리한다. **이 문서의 항목은 아직 수정하지 않았다 — 발견 기록과 권장안이다.**
치명적(H급) 결함은 추가로 발견하지 못했다. 중간 수준(M) 5건, 소소(L) 다수.

각 항목은 **발견 → 영향 → 권장 수정** 순서로 기술한다.

---

# 중간 수준 (M) — 고칠 가치가 있는 것들

## M1. 커밋 성공 후에도 Rollback을 호출하는 예외 처리

**위치**: `RTWWebServer/Services/GameEntryService.cs:30-58`

### 발견
`EnterGameAsync`가 `BeginTransactionAsync` → ... → `CommitAsync()` 이후에
트랜잭션 범위 **밖의** 작업인 `userSessionProvider.CreateSessionAsync`(Redis 쓰기)를
같은 try 블록 안에서 수행한다. catch는 무조건 `RollbackAsync()`를 호출한다.

```csharp
await transaction.CommitAsync();
var userSession = await userSessionProvider.CreateSessionAsync(user.Id); // 커밋 후
return userSession;
}
catch
{
    await transaction.RollbackAsync(); // 이미 커밋된 트랜잭션에 호출
    throw;
}
```

### 영향
Redis 장애로 `CreateSessionAsync`가 던지면, 이미 완료된 트랜잭션에 대해 `RollbackAsync`가
호출된다. MySqlConnector는 이때 `InvalidOperationException`("This MySqlTransaction has
completed...")을 던지므로 **원래 예외(Redis 장애)가 가려지고**, 클라이언트는 원인 불명의
500을 받는다. DB 데이터는 이미 커밋되어 정상이므로 롤백 의도 자체도 잘못됐다.

### 권장 수정
세션 생성을 try/catch(트랜잭션 범위) **밖으로** 옮긴다. 트랜잭션은 DB 작업만 감싸고,
커밋 성공 후의 Redis 작업은 별도 단계로 분리한다.

---

## M2. 인증 엔드포인트에 rate limiting이 전혀 없음

**위치**: `RTWWebServer/Controllers/LoginController.cs`, `AccountController.cs`

### 발견
`/Login/login`, `/Account/createAccount`, `/Account/createGuestAccount`에 어떤 속도 제한도
없다. 상수 시간 비교(`FixedTimeEquals`)는 타이밍 부채널만 막을 뿐, 온라인 무차별 대입은
그대로 가능하다.

### 영향
- `/Login/login`: 비밀번호 무차별 대입·크리덴셜 스터핑에 무방비.
- `/Account/createGuestAccount`: 익명으로 무한정 계정 행 생성(DB 팽창, 게스트 GUID 고갈은
  아니지만 저장소·인덱스 오염).
- 현재 웹서버에 남은 **가장 큰 보안 공백**.

### 권장 수정
ASP.NET Core 내장 `builder.Services.AddRateLimiter(...)` + `app.UseRateLimiter()`로
IP(또는 이메일) 기준 고정/슬라이딩 윈도우 제한을 건다. 로그인은 분당 수 회, 게스트 계정
생성은 IP당 더 엄격하게. 프록시 뒤라면 `X-Forwarded-For` 신뢰 설정(`ForwardedHeaders`)도
함께 필요.

---

## M3. 내부 전용 패킷을 클라이언트가 보낼 수 있음

**위치**: `RTWServer/Game/Packet/GamePacketFactory.cs:10-29`

### 발견
와이어 바이트를 역직렬화하는 `CreatePacket(int, ReadOnlySpan<byte>)`이 모든 PacketId를
허용한다 — 서버→클라 전용(`SChat`, `SAuthResult` 등)과 **내부 전용 `ISessionClosed`**까지.
`ISessionClosed`는 `ClientSessionManager`가 세션 정리용으로만 만들어 쓰는 패킷이다.

### 영향
클라이언트가 `ISessionClosed`를 직접 전송하면 연결이 유지된 채로
`GamePacketHandler.HandleSessionClosed` → `_chatService.CleanupSession`이 실행되어
자신의 모든 방 멤버십이 제거된다. 피해 범위가 자기 세션에 한정돼 실제 위험은 낮지만,
**신뢰 경계 위반**(클라가 서버 내부 제어 패킷을 주입)이라는 설계 결함이다.

### 권장 수정
역직렬화 팩토리가 **클라이언트→서버 패킷(C-접두)만** 화이트리스트로 허용하도록 좁힌다.
`ISessionClosed`/`S`-접두 패킷은 와이어 역직렬화 대상에서 제외하고, 알 수 없거나 허용되지
않은 PacketId는 기존처럼 세션 종료로 처리한다.

---

## M4. 조회/가챠 경합으로 stale 캐시가 24시간 잔존 가능

**위치**: `RTWWebServer/Services/CharacterGachaService.cs:96-127`,
`RTWWebServer/Cache/CacheManager.cs:27-35`

### 발견
`GET /Character/owned`는 `RequestLockingMiddleware`에서 의도적으로 락을 건너뛴다(읽기
최적화, 2026-06-12 변경). 다음 인터리빙이 가능하다:

1. GET이 캐시 미스로 DB에서 캐릭터 목록을 읽음 (아직 Redis에 안 씀)
2. 가챠(POST)가 캐릭터 추가 커밋 + `InvalidatePlayerCharactersCacheAsync`로 Redis 키 삭제
3. GET이 `CommitAllChangesAsync`에서 **1단계의 옛 목록**을 Redis에 기록

무효화가 무력화되고, `CacheManager.Set` → `DistributedCacheAdapter.SetAsync`의 기본 TTL인
**24시간** 동안 stale 목록이 남는다. `ICacheManager.Set`에 TTL 파라미터가 없어 짧은 TTL도
줄 수 없다.

### 영향
방금 뽑은 캐릭터가 목록에서 최대 24시간 누락되어 보일 수 있다. 재화는 DB 기준으로 차감하므로
화폐 손실은 없으나(2026-06-12에 수정한 부분), 소유 목록 표시가 어긋난다.

### 권장 수정
- 단기: `ICacheManager.Set(key, value, TimeSpan? ttl)` 오버로드를 추가하고 캐릭터 목록 캐시에
  짧은 TTL(수 분)을 지정 — stale 잔존 시간을 24h에서 분 단위로 축소.
- 정석: 읽기 경로도 무효화와 정합하도록, GET을 락 범위에 포함하거나, 캐시 채우기를
  "쓰기 후 무효화"와 직렬화되는 단일 지점으로 옮긴다(예: 무효화 시 짧은 tombstone).

---

## M5. 기본 닉네임 `User_{accountId}` 선점 시 영구 입장 불가

**위치**: `RTWWebServer/Services/GameEntryService.cs:61-79`,
`RTWWebServer/Services/UserService.cs:64-82`

> 2026-06-12 보고서의 '남은 과제'에 한 줄로 언급됐으나, 구체적 실패 경로를 여기 기록한다.

### 발견
신규 유저 생성 시 닉네임을 `User_{accountId}`로 고정한다. 한편 `UpdateNicknameAsync`는
이 패턴을 예약어로 막지 않으므로, 임의의 유저가 닉네임을 `"User_123"`으로 바꿀 수 있다.

이후 account 123이 처음 `/Game/enter`를 호출하면 `CreateNewUser`가 `User_123`으로 INSERT를
시도 → `uk_nickname` 유니크 제약 위반 → `DbUpdateException`. `GameEntryService`에는 이
예외를 잡는 코드가 없어 **처리되지 않은 채 500**으로 떨어진다. 재시도해도 동일하게 실패하므로
account 123은 **영구히 게임 입장 불가**.

### 영향
공격자가 `User_{N}` 형태 닉네임을 선점하는 것만으로 특정 account의 최초 입장을 영구 차단할 수
있다(타깃 DoS). 우연한 충돌도 가능.

### 권장 수정
- `UpdateNicknameAsync`에서 `^User_\d+$` 패턴을 예약어로 거부, **그리고**
- `CreateDefaultCharacterForNewUserAsync`처럼 유저 생성도 `DuplicateKeyEntry`를 잡아
  대체 닉네임(예: `User_{accountId}_{짧은난수}`)으로 재시도하거나, 처음부터 충돌 불가능한
  형식을 사용한다. 두 방어를 함께 적용해야 우연 충돌과 의도적 선점을 모두 막는다.

---

# 소소한 것들 (L)

## L1. Unlock 예외 미처리
`RTWWebServer/Middlewares/RequestLockingMiddleware.cs:41-47` — finally의
`UnlockAsync`에서 Redis가 죽으면, 응답 본문이 이미 시작된 뒤 예외가 던져져 깨진 응답이 된다.
락은 30초 TTL로 자동 해제되므로, try/catch + 경고 로그로 감싸 언락 실패가 요청을 깨지 않게 한다.

## L2. 인증 체크 누락 패킷
`RTWServer/Game/Packet/GamePacketHandler.cs` — `EchoMessage`(line 24)는 미인증 클라이언트도
무제한 호출 가능하고, `CChatLeave` 핸들러(line 176)만 다른 채팅 핸들러와 달리
`IsAuthenticated` 검사가 없다. 일관성을 위해 인증 게이트를 통일한다.

## L3. 죽은 코드
호출처가 없는 멤버들:
- `JwtTokenProvider.ValidateJwt` / `ParseJwtToken`과 `JwtTokenInfo` DTO — 런타임 검증은
  JwtBearer 미들웨어가 대신하므로, 테스트(`JwtTokenProviderTests`)에서만 사용된다.
- `CacheManager.RollbackAllChanges`
- `UserRepository.GetAllAsync` / `GetByMainCharacterIdAsync` / `IsNicknameTakenAsync`
- `AccountRepository.GetByIdAsync` / `Update` / `Delete`
- `PlayerCharacterRepository.GetByUserIdAndCharacterIdAsync` / `UpdateAsync`

제거하거나, 의도된 공개 API라면 그 이유를 주석으로 남긴다.

## L4. PBKDF2 반복 횟수와 버전 없는 해시 포맷
`RTWWebServer/Providers/Authentication/PasswordHasher.cs` — 반복 100,000회는 OWASP 권장값
(PBKDF2-SHA256 기준 600,000)보다 낮다. 더 큰 문제는 저장 해시에 알고리즘/반복횟수/버전
메타데이터가 없어, 향후 파라미터를 올릴 때마다 **전체 계정이 또 무효화**된다는 점이다(2026-06-11
PBKDF2 전환 때 한 번 겪음). `{버전}.{반복횟수}.{salt}.{hash}` 같은 자기서술적 포맷을 먼저
도입해 점진적 재해싱(로그인 성공 시 최신 파라미터로 재계산) 경로를 열어둘 것을 권한다.

## L5. 운영 편의
- 읽기 전용 EF 쿼리에 `AsNoTracking()` 미적용 — 변경 추적 오버헤드. `GetByIdAsync` 등은
  쓰기에도 쓰이므로 메서드 분리 후 적용.
- `HttpContext.RequestAborted` 토큰이 서비스/EF/Redis 호출로 전파되지 않음 — 클라이언트가
  끊어도 서버 작업이 끝까지 진행된다.
- 헬스체크 엔드포인트 부재 — Docker/오케스트레이터 운영 시 `AddHealthChecks` + `/health`가 유용.
- `appsettings.json`의 기본 로그 레벨이 `Debug` — 프로덕션 기준 과다(민감정보 노출·성능).
  환경별 분리 권장.

---

# 이전부터 알려진 미해결 항목 (변동 없음)

- **게임 서버 인증 스텁**: 비어 있지 않은 토큰을 모두 통과. 웹서버 세션(`session_{userId}`)과
  대조하려면 `CAuthToken`에 userId 필드가 필요한데 `.proto` 원본이 리포에 없어 막혀 있다.
- git 히스토리의 옛 시크릿 로테이션.
- 마스터데이터 런타임 리로드 시 검증 예외가 OptionsMonitor 내부(콜백 이전)에서 발생하는 경로.
- 가챠 `count` 상한 부재·`gachaType` 미사용.

---

# 우선순위 제안

| 순위 | 항목 | 근거 | 변경 범위 |
|---|---|---|---|
| 1 | M1 | 작은 수정으로 장애 원인 은폐 제거 | 작음 |
| 2 | M5 | 타깃 DoS, 우연 충돌 모두 차단 | 작음 |
| 3 | M3 | 신뢰 경계 위반, 화이트리스트만 추가 | 작음 |
| 4 | M2 | 가장 큰 보안 공백이나 인프라 결정 필요 | 중간 |
| 5 | M4 | 인터페이스 변경(Set TTL) 동반 | 중간 |

M1·M3·M5는 변경 범위가 작아 한 묶음으로 처리 가능하다. M2(rate limiter)와 M4(TTL 파라미터)는
인터페이스·구성이 바뀌므로 별도 작업으로 분리한다.

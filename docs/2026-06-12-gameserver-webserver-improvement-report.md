# RTW 게임 서버 · 웹서버 개선 보고서 (2026-06-12)

전체 코드베이스 리뷰에서 발견한 사항을 10개 커밋(`c5e68ac`~`9fca764`)으로 수정했다.
게임 서버(RTWServer) 4건, 웹서버(RTWWebServer) 6건.
각 항목은 **기존 동작 → 개선 동작 → 대안과 선택 이유** 순서로 기술한다.

---

# 게임 서버 (RTWServer)

## 1. Accept 루프 생존성 — `c5e68ac`

### 기존 동작
`AsyncAwaitServer.Start`의 `catch (SocketException)`이 `while` 루프 **바깥**에 있었다.

- **단점**: 클라이언트가 핸드셰이크 중 연결을 리셋하는 등 개별 연결의 일시적 소켓 오류 한 번이면 accept 루프 전체가 종료 → 기존 세션은 유지되지만 **신규 연결을 영원히 받지 못하는 반쯤 죽은 서버**가 됨.

### 개선 동작
`AcceptClientAsync` 호출을 루프 안의 try/catch로 감싸 `SocketException`은 경고 로그 후 `continue`, `OperationCanceledException`(정상 종료 신호)은 `break`.

- **장점**: 한 클라이언트의 비정상 연결이 서버 가용성에 영향을 주지 못함. 종료 의도(취소 토큰)와 장애(소켓 오류)의 처리 경로가 분리됨.

### 대안과 선택 이유
연속 실패 횟수 제한(N회 초과 시 서버 종료) 같은 서킷 브레이커도 고려할 수 있으나, accept 실패가 무한 반복되는 시나리오(포트 회수 등)는 어차피 리스너 예외로 별도 표면화된다. 현 단계에서는 단순 continue가 적절.

---

## 2. stdin EOF 종료 처리 — `cff1401`

### 기존 동작
콘솔 입력 루프가 `Console.ReadLine()` 결과를 `"quit"`하고만 비교.

- **단점**: stdin이 닫힌 환경(Docker detached, 데몬, 파이프 종료)에서는 `ReadLine()`이 즉시 `null`을 반환하므로 **무한 busy-loop로 CPU 코어 하나를 100% 점유**.

### 개선 동작
`null`(EOF)도 종료 신호로 처리하고, 종료 사유(quit 명령 vs stdin closed)를 로그에 남김.

### 대안과 선택 이유
`IHostApplicationLifetime` 기반 Generic Host로 전환하면 콘솔 의존 자체가 사라지지만, 이는 게임 서버의 호스팅 구조 전체를 바꾸는 일이라 별도 작업으로 미룸. 현재 구조에서의 최소 수정을 택했다.

---

## 3. PlayerId 발급과 송신 경로 — `fb881fe`

### 기존 동작
- PlayerId를 `sessionId.GetHashCode()`로 생성 (3곳: ClientSession, GamePacketHandler, ChatService).
- `SendAsync`가 송신 큐 플러시(`FlushSendQueueAsync`)의 완료까지 await.
- 인증 토큰 원문을 Debug 로그에 기록.

- **단점**:
  - .NET의 문자열 해시는 **프로세스마다 랜덤화**되어 서버 재시작 시 같은 세션이라도 PlayerId가 바뀌고, 서로 다른 세션이 같은 해시를 가질 수 있음(충돌).
  - 수신이 느린 클라이언트의 TCP 백프레셔가 `FlushAsync`를 막으면, 그 세션에 보내는 **모든 호출자가 함께 멈춤** — 브로드캐스트(`Task.WhenAll`)는 물론, 그것을 await하는 **발신자 세션의 수신 루프까지** 정지.
  - 토큰이 로그에 남아 로그 접근 = 자격증명 탈취 (웹서버에서는 이미 제거한 패턴이 게임 서버에 잔존).

### 개선 동작
- PlayerId를 세션 생성 시 `Interlocked.Increment` 카운터로 발급, `IClientSession.PlayerId`로 노출. `GetHashCode()` 사용처 전부 제거.
- `SendAsync`는 큐 적재 후 플러시를 **fire-and-forget**으로 시작 (`_isSending` 게이트로 드레이너는 단일 유지, 패킷 순서 보존). 큐가 256개를 넘으면 느린 소비자로 판단해 세션 종료 — 무한 메모리 적재 방지.
- 토큰 로깅 제거.

### 대안과 선택 이유
| 대안 | 판단 |
|---|---|
| (a) `System.Threading.Channels` 기반 송신 워커 | 정석이지만 기존 `_sendQueue`+`_isSending` 구조가 이미 단일 드레이너를 보장하므로, await 한 줄을 떼는 것으로 같은 효과 → 최소 변경 선택 |
| (b) 큐 상한 초과 시 오래된 패킷 드롭 | 채팅 패킷 유실은 허용 가능하나 인증 응답 같은 제어 패킷 유실은 위험. 게임 서버 관례대로 **느린 클라이언트는 끊는 것**이 안전 |
| (c) PlayerId를 웹서버의 실제 userId로 | 근본 해결이지만 인증 연동(아래 '보류 항목')이 선행 필요 |

`FlushSendQueueAsync`는 내부에서 모든 예외를 잡아 세션 종료로 수렴시키므로 fire-and-forget이어도 예외 유실이 없다.

---

## 4. 채팅방 수명주기와 기본 방 라우팅 — `753fbb1`

### 기존 동작
- `JoinRoomAsync`가 클라이언트가 보낸 임의 `roomId`로 방을 생성. `RemoveRoom`은 **어디서도 호출되지 않아** 빈 방이 영구 잔존.
- `CChat`/`CChatChat`(proto에 RoomId 필드 없음)은 무조건 기본 방("global")으로 라우팅되는데, 유저를 global에 넣어주는 코드가 없어 멤버십 검사에 걸려 **메시지가 조용히 버려짐** — 클라이언트는 실패 응답조차 받지 못함.
- 채팅 발신자 정보를 `sessionId.GetHashCode()`와 세션 ID 문자열로 즉석 생성.

- **단점**: 악의적 클라이언트가 `/join`을 반복해 방을 무제한 생성(메모리 고갈). 기본 채팅이 동작하지 않음.

### 개선 동작
- `ChatRoomManager`를 단일 락 기반으로 재작성:
  - 마지막 멤버가 나가면 방 자동 제거 (영구 방으로 표시된 global은 제외)
  - 방 개수 상한 1,000개, roomId 길이 상한 64자
  - `CreateRoom` → `GetOrCreateRoom(roomId, roomName, isPersistent)` (한도 초과·잘못된 ID는 null 반환, 예외로 세션을 죽이지 않음)
- 인증 성공 시 기본 방에 **자동 입장** → `CChat`/`CChatChat`이 즉시 동작.
- 발신자 정보(PlayerId, Name)는 방에 저장된 `IPlayer`에서 조회 (`IChatRoom.TryGetMember` 신설).

### 대안과 선택 이유
- **락 vs ConcurrentDictionary**: 기존 `ConcurrentDictionary`만으로는 "마지막 멤버 퇴장 확인 → 방 제거" 사이에 다른 스레드의 입장이 끼어드는 race를 막을 수 없다(제거된 방에 입장해 메시지 유실). 채팅 입장/퇴장은 저빈도 작업이라 명시적 락의 비용이 무시 가능하고 정확성을 얻는다. 브로드캐스트 자체는 방 내부의 `ConcurrentDictionary`를 그대로 사용하므로 락 밖에서 수행된다.
- **방별 채팅의 한계**: `CChatChat`에 RoomId를 추가하는 proto 변경이 정석이지만, 리포에 `.proto` 원본이 없고 생성된 `Packet.cs`만 있어 재생성이 불가. 자동 입장은 현재 제약 안에서 기본 채팅을 살리는 차선책이다.

---

# 웹서버 (RTWWebServer)

## 5. 마스터데이터 검증기 등록 — `33ff6e2`

### 기존 동작
`MasterDataOptionsValidator`(중복 캐릭터 ID, DataAnnotations 검사)를 `AddSingleton<MasterDataOptionsValidator>()`로 **구체 타입으로만 등록**.

- **단점 (실버그)**: 옵션 시스템은 `IEnumerable<IValidateOptions<T>>`로 검증기를 찾으므로 이 등록을 **인식하지 못함** → `ValidateOnStart()`가 아무것도 검증하지 않았다. 검증기와 그 테스트는 존재하지만 실제로는 한 번도 실행된 적 없는 죽은 코드였던 셈. 중복 ID가 들어오면 검증기 대신 `MasterDataProvider`의 `ToImmutableDictionary`가 의미 불명의 `ArgumentException`으로 터지고, `reloadOnChange` 리로드 시점이라면 파일 워처 콜백의 미처리 예외로 **프로세스가 죽을 수 있음**.

### 개선 동작
- `AddSingleton<IValidateOptions<MasterDataOptions>, MasterDataOptionsValidator>()`로 교체 → 시작 시 검증이 실제로 동작.
- 런타임 리로드의 `BuildSnapshot`을 try/catch로 감싸 실패 시 **기존 스냅샷 유지** + 에러 로그. 시작 시에는 그대로 던져 fail-fast.

### 대안과 선택 이유
시작과 리로드의 실패 정책을 다르게 가져갔다: 시작 시 잘못된 데이터는 배포 실수이므로 즉시 죽는 것이 맞고, 운영 중 리로드 실패는 "이전 정상 데이터로 계속 서비스"가 맞다. 잔여 위험으로, 리로드 시 검증 예외가 우리 콜백 이전(OptionsMonitor 내부)에서 발생하는 경로가 남아 있다 — '남은 과제' 참조.

---

## 6. Swagger 보안 정의 — `2eb60a4`

### 기존 동작
`AddSwaggerGen()` 기본 설정. Bearer JWT와 `X-User-Id`/`X-Auth-Token` 세션 헤더에 대한 security definition이 없었다.

- **단점**: Swagger UI에서 인증이 필요한 엔드포인트(사실상 전부)를 테스트할 수 없어, 개발자가 curl이나 별도 도구로 우회해야 했음.

### 개선 동작
Bearer(http/JWT) + 세션 헤더 2종(apiKey)을 security scheme으로 정의하고 전역 requirement로 연결. Swagger UI의 Authorize 버튼에서 값을 넣으면 모든 요청에 자동 첨부된다.

### 대안과 선택 이유
`IOperationFilter`로 엔드포인트별 스킴([Authorize] 속성 기준)을 정밀 표시하는 방법도 있으나, 컨트롤러 5개 규모에서는 전역 requirement의 단순함이 낫다. 스킴이 늘어나면 그때 필터로 전환.

---

## 7. RequireHttpsMetadata 환경 분기 — `d79fc9f`

### 기존 동작
`options.RequireHttpsMetadata = false`가 무조건 적용 (주석은 "개발 환경에서는 false"라고 적혀 있으나 분기 없음).

### 개선 동작
`AddJwtAuthentication`이 `isDevelopment`를 받아 `!isDevelopment`로 설정 — 주석과 코드가 일치하게 됨.

### 대안과 선택 이유
현재 구성은 symmetric key 직접 지정이라 Authority 메타데이터 조회가 없어 실질 영향은 작지만, 나중에 OIDC Authority를 붙일 때 안전하지 않은 기본값이 남아 있지 않도록 미리 정리했다. 비용이 0에 가까운 방어.

---

## 8. 가입 검증과 중복 이메일 — `37b4d7c`

### 기존 동작
가입 시 null/공백 검사만 존재. `Email`에 unique 인덱스는 있으나 위반 시 `DbUpdateException`이 그대로 올라가 **500 + InternalServerError(1999)**.

- **단점**: "이미 가입된 이메일"은 정상적인 사용자 실수인데 서버 장애로 보임 — 닉네임 중복(`DuplicateNickname=1010`)은 이미 처리하면서 이메일만 빠져 있던 비대칭. 형식 검증이 없어 64자 초과 이메일도 DB 에러로 떨어짐.

### 개선 동작
- 이메일 형식(`EmailAddressAttribute`) + 길이(64자, DB 컬럼과 일치) 검증, 비밀번호 최소 8자.
- `DuplicateKeyEntry`를 잡아 신규 에러 코드 **`DuplicateEmail(1011)`** 반환 — UserService의 닉네임 처리와 같은 패턴.

### 대안과 선택 이유
사전 존재 검사(`FindByEmailAsync` 후 분기)를 추가하는 방법도 있지만, 어차피 race 때문에 DB 제약 + 예외 변환은 필수다. 가입은 저빈도 작업이라 사전 조회의 UX 이점이 작아 **예외 변환만**으로 충분하다고 판단(닉네임은 변경 빈도가 높아 사전 검사를 유지한 것과 대비). 비밀번호 정책은 최소 길이만 — 복잡도 규칙(대소문자/특수문자 강제)은 NIST 800-63B도 권장하지 않는다.

---

## 9. 로그인 실패 통일과 상수 시간 비교 — `31fb008`

### 기존 동작
- 이메일 없음 → `InvalidEmail(1001)`, 비밀번호 불일치 → `InvalidPassword(1002)`로 **구분 응답**.
- 해시 비교가 `!=` 문자열 비교, 세션 토큰 비교도 `!=`.
- 게스트 계정(Password/Salt가 null)의 이메일로 로그인 시도 시 `Convert.FromBase64String(null)`이 던져 **500**.

- **단점**: 에러 코드 구분으로 **계정 존재 여부를 열거**할 수 있음(스팸·크리덴셜 스터핑의 사전 작업). 조기 종료 문자열 비교는 이론상 타이밍 부채널.

### 개선 동작
- 모든 로그인 실패(미존재/불일치/게스트 계정)를 신규 코드 **`InvalidCredentials(1012)`** 하나로 통일.
- 해시·세션 토큰 비교를 `CryptographicOperations.FixedTimeEquals`로 교체.
- 자격증명 없는 계정은 비교 시도 전에 실패 처리 (500 → 정상 실패 응답).

### 대안과 선택 이유
기존 코드(1001/1002)를 재사용하지 않고 새 코드를 만든 이유: 어느 쪽이든 기존 코드를 재사용하면 의미가 왜곡되고, 클라이언트가 옛 코드로 분기하던 로직이 조용히 오동작한다. **명시적으로 깨뜨리고 새 코드로 마이그레이션**하는 쪽이 안전. 미존재 계정 경로는 해시 계산을 건너뛰므로 응답 시간 차이로 존재 여부를 추측할 여지가 이론상 남는데, 더미 해시 계산으로 시간을 맞추는 방어는 현 단계 위협 모델에서 과잉이라 보류.

---

## 10. 읽기 요청 락 생략 — `9fca764`

### 기존 동작
`RequestLockingMiddleware`가 인증된 **모든** 요청에 Redis 분산락을 적용.

- **단점**: `GET /Character/owned` 같은 읽기에도 요청마다 Redis 라운드트립 2회(lock/unlock)가 붙고, 같은 유저의 읽기·쓰기가 전부 직렬화됨. 읽기는 상태를 바꾸지 않으므로 직렬화로 보호할 대상이 없다.

### 개선 동작
GET/HEAD/OPTIONS는 락 없이 통과. 쓰기(POST 등)는 기존대로 유저/계정 락. 삭제된 `UserAuthenticationMiddleware`를 언급하던 스테일 주석도 현행(SessionAuthenticationHandler)으로 수정.

### 대안과 선택 이유
| 대안 | 판단 |
|---|---|
| (a) 엔드포인트 어트리뷰트로 opt-in (`[RequireUserLock]`) | 가장 정밀하지만 새 엔드포인트에서 깜빡하면 보호가 빠지는 opt-in의 위험. 현재는 "쓰기 = 보호"가 전 엔드포인트에서 참이라 메서드 기준이 더 안전한 기본값 |
| (b) 현상 유지 | 읽기 비중이 큰 게임 API에서 불필요한 Redis 부하와 지연 누적 |

읽기인데 상태를 바꾸는 엔드포인트가 생기면 그 시점에 (a)로 전환한다. HTTP 의미론상 GET은 안전 메서드여야 하므로 그런 엔드포인트 자체가 설계 오류이기도 하다.

---

## 검증

- `dotnet build` 경고 0, 에러 0
- 단위 테스트 **63개 전체 통과** (READ 요청 락 생략 테스트 1개 신규)
- 커밋 단위 구성: 한 파일에 여러 주제가 섞인 경우(`RTWServer/Program.cs`, `DependencyInjectionExtensions.cs`) hunk 단위로 분리 스테이징했고, 각 커밋 시점의 트리가 컴파일되도록 순서를 맞춤 (예: PlayerId 커밋 → 채팅 커밋)

## 클라이언트 영향 (Breaking)

1. 로그인 실패 에러 코드: `1001`/`1002` → **`1012`(InvalidCredentials)** 통일
2. 가입 검증 추가: 이메일 형식·64자 제한, 비밀번호 8자 이상 (`1005`), 중복 이메일은 **`1011`(DuplicateEmail)**
3. 게임 서버: 인증 성공 시 기본 채팅방("global") 자동 입장
4. 게임 서버: 송신 큐 256개 초과 시(수신 정체) 서버가 연결을 끊음

## 남은 과제

- **게임 서버 인증이 여전히 스텁**: 비어 있지 않은 토큰을 모두 통과시킨다. 웹서버 세션(`session_{userId}`)과 대조하려면 `CAuthToken`에 userId 필드가 필요한데, **`.proto` 원본이 리포에 없어**(생성된 `Packet.cs`만 존재) proto 재생성이 막혀 있다. 원본 확보가 선행 과제.
- 마스터데이터 **런타임 리로드** 시 잘못된 데이터의 검증 예외가 OptionsMonitor 내부(우리 콜백 이전)에서 발생하는 경로가 남음 — 리로드가 필요 없다면 `reloadOnChange: false`가 가장 단순한 해법.
- git 히스토리의 옛 시크릿 로테이션 (이전 보고서에서 이월)
- 가챠 `count` 상한·`gachaType` 미사용, `User_{accountId}` 기본 닉네임의 선점 충돌 등 설계 판단이 필요한 항목들

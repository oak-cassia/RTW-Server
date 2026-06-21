# RTW TypeScript 테스트 클라이언트

RTWWebServer의 REST API를 손으로/자동으로 두드려 보는 **의존성 0** TypeScript 클라이언트다.
Node 22.6+의 내장 TypeScript 실행(`node foo.ts`)과 내장 `fetch`만 쓰므로 `npm install` 없이 바로 돌아간다.

세 가지 사용 방식이 있다(모두 같은 `src/api.ts` SDK를 공유):

| 방식 | 실행 | 설명 |
| --- | --- | --- |
| **웹 (브라우저)** | `npm run web` → http://localhost:8080 | 게임 같은 대시보드 UI. 로비 그리드, 전투 로그, 가챠 등을 눈으로 확인 |
| **대화형 콘솔** | `npm start` | 터미널 REPL |
| **스모크** | `npm run smoke` | 비대화형 엔드투엔드 점검 |

## 웹 클라이언트 (브라우저)

```bash
cd ts-client
npm run web                                   # http://localhost:8080
RTW_BASE_URL=http://localhost:5080 npm run web # 백엔드 주소 지정
PORT=9000 npm run web                          # 웹 서버 포트 변경
```

`web/serve.ts`는 의존성 0 개발 서버다. 두 가지를 한다:

1. **TS → JS 온더플라이 변환** — `.ts` 요청이 오면 Node 내장 `stripTypeScriptTypes`로 타입을 벗겨
   브라우저가 그대로 `import` 할 JS로 돌려준다. 빌드 단계가 없고, `src/api.ts`·`src/types.ts`를
   브라우저에서 **그대로** 재사용한다.
2. **API 프록시** — 정적 경로가 아닌 모든 요청을 백엔드로 흘려보낸다. 클라가 백엔드와 같은
   오리진이 되므로 **서버 CORS 설정이 필요 없다**.

구성: `web/index.html`(UI), `web/styles.css`, `web/app.ts`(UI ↔ SDK 연결), `web/serve.ts`(개발 서버).

(Node 환경의 `cli.ts`/`smoke.ts`는 브라우저 없이 Node에서 직접 돌므로 마찬가지로 CORS와 무관하다.)

## 사전 준비

- Node **22.6 이상** (권장: 23.6+ 또는 current). `node -v`로 확인.
- RTWWebServer가 떠 있어야 한다. 기본 주소는 `http://localhost:5000`.
  - 서버는 MySQL + Redis에 의존하므로 둘 다 기동되어 있어야 정상 동작한다.
  - 다른 주소면 `RTW_BASE_URL` 환경변수로 지정한다.

## 실행

```bash
cd ts-client

# 대화형 콘솔
node src/cli.ts
# 또는
npm start

# 비대화형 스모크(전체 흐름 한 번에 점검)
node src/smoke.ts
# 또는
npm run smoke

# 서버 주소 바꾸기
RTW_BASE_URL=http://localhost:5080 node src/smoke.ts
```

선택: 타입 체크가 필요하면 `npm install` 후 `npm run typecheck`. (실행 자체에는 불필요)

## 인증 흐름

서버는 두 개의 인증 스킴을 쓴다. 클라이언트가 이 단계를 대신 관리한다.

1. `POST /Account/createGuestAccount` → 게스트 GUID
2. `POST /Login/guestLogin` (또는 이메일 `/Login/login`) → **JWT**
3. `POST /Game/enter` (`Authorization: Bearer <JWT>`) → **세션** `{ userId, token, nickname }`
4. 이후 모든 호출 → 헤더 `X-User-Id`, `X-Auth-Token`

대화형 콘솔에서 `g` 한 번이면 1~3을 자동으로 처리한다.

## 대화형 명령

| 명령 | 설명 |
| --- | --- |
| `g` | 게스트 빠른 시작 (생성→로그인→입장) |
| `signup` / `login` / `enter` | 이메일 계정 흐름 단계별 |
| `nick` | 닉네임 변경 (UserInfo 반환 = 내 정보 조회 대체) |
| `lobby` / `savelobby` / `expand` | 로비 조회 / 가구 배치 / 방 확장 |
| `gacha` / `chars` | 가챠 / 보유 캐릭터 |
| `mstart` / `mend` / `mission` | 임무 시작 / 정산 / 한 번에 |
| `logout` | 로그아웃 |
| `raw` | 임의 요청 (METHOD PATH [JSON]) |
| `state` / `help` / `q` | 상태 / 도움말 / 종료 |

## 구조

```
src/
  types.ts   C# DTO/enum 미러 + 에러코드 이름 변환
  api.ts     RtwClient — 타입드 API SDK (Node·브라우저 공용)
  cli.ts     대화형 콘솔 (Node)
  smoke.ts   비대화형 엔드투엔드 점검 (Node)
web/
  index.html 게임 대시보드 UI
  styles.css 다크 테마 스타일
  app.ts     UI ↔ RtwClient 연결 (브라우저)
  serve.ts   의존성 0 개발 서버 (TS 스트립 + API 프록시)
```

## 알려진 제약

- **읽기 전용 "내 정보" 엔드포인트가 없다.** 현재 스탯/재화는 `nick`(닉네임 변경 응답),
  가챠 응답, 임무 정산 응답으로만 확인할 수 있다.
- **`long` 정밀도:** 임무 `seed`는 C# `long`이라 2^53을 넘으면 표시값 정밀도가 깨질 수 있다.
  seed는 표시용일 뿐 서버로 되돌려 보내지 않으므로 동작에는 영향이 없다.
- **임무 시작이 `InvalidArgument`로 실패할 수 있다.** 신규 유저는 `MainCharacterId`가 0으로
  생성되는데(서버 측), `/Mission/start`는 이 값으로 캐릭터 마스터를 찾으므로 실패한다.
  서버에서 기본 메인 캐릭터를 지정해야 임무 흐름이 끝까지 돈다.
```

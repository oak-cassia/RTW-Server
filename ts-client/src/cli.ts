// 대화형 테스트 콘솔. 명령을 입력하면 해당 API를 호출하고 결과를 보여준다.
// 실행: node src/cli.ts   (서버 주소는 RTW_BASE_URL 환경변수로 변경, 기본 http://localhost:5000)

import { createInterface } from "node:readline/promises";
import { stdin, stdout, env } from "node:process";
import { RtwClient, ApiError } from "./api.ts";
import { missionOutcomeName, statKindName } from "./types.ts";
import type { MissionResultDto } from "./types.ts";

const BASE_URL = env.RTW_BASE_URL ?? "http://localhost:5000";

const HELP = `
명령 목록:
  g           게스트 빠른 시작 (계정 생성 → 로그인 → 입장)
  signup      이메일 계정 생성
  login       이메일 로그인 (입장은 별도로 enter)
  enter       게임 입장 (JWT 필요) → 세션 발급
  nick        닉네임 변경 (UserInfo 반환 = 내 정보 스냅샷)
  lobby       로비 조회
  savelobby   가구 1개 배치 저장
  expand      방 확장
  gacha       캐릭터 가챠
  chars       보유 캐릭터 조회
  mstart      임무 시작 (티켓 발급)
  mend        임무 정산 (보상 지급)
  mission     임무 시작 + 정산 한 번에
  logout      로그아웃
  raw         임의 요청 (METHOD PATH [jsonBody])
  state       현재 세션 상태
  help        이 도움말
  q           종료
`;

function show(label: string, value: unknown): void {
  console.log(`\n── ${label} ──`);
  console.log(typeof value === "string" ? value : JSON.stringify(value, null, 2));
}

function printError(e: unknown): void {
  if (e instanceof ApiError) {
    const http = e.httpStatus > 0 ? ` [HTTP ${e.httpStatus}]` : "";
    console.error(`⚠️  요청 실패: ${e.message}${http}`);
  } else {
    console.error(`⚠️  오류: ${(e as Error)?.message ?? String(e)}`);
  }
}

function printMissionResult(r: MissionResultDto): void {
  show(`임무 결과: ${missionOutcomeName(r.outcome)}`, {
    fameGained: r.fameGained,
    goldGained: r.goldGained,
    newFame: r.newFame,
    newGold: r.newGold,
    newStamina: r.newStamina,
    seed: r.seed,
  });
  for (const e of r.log) {
    const mark = e.passed ? "✅" : "❌";
    console.log(
      `  ${mark} [${e.index}] ${e.stage} (${statKindName(e.stat)}) roll=${e.roll}/${e.required} ` +
        `mental=${e.mentalAfter} — ${e.message}`,
    );
  }
}

function printState(client: RtwClient): void {
  show("세션 상태", {
    baseUrl: client.baseUrl,
    hasJwt: client.jwt !== null,
    userId: client.userId,
    nickname: client.nickname,
    sessionReady: client.isSessionReady,
  });
}

async function main(): Promise<void> {
  const client = new RtwClient(BASE_URL);
  const rl = createInterface({ input: stdin, output: stdout });

  const num = async (prompt: string, fallback: number): Promise<number> => {
    const raw = (await rl.question(`${prompt} [${fallback}]: `)).trim();
    if (raw === "") return fallback;
    const n = Number(raw);
    return Number.isFinite(n) ? n : fallback;
  };
  const str = async (prompt: string, fallback = ""): Promise<string> => {
    const raw = (await rl.question(fallback ? `${prompt} [${fallback}]: ` : `${prompt}: `)).trim();
    return raw === "" ? fallback : raw;
  };

  console.log("RTW 테스트 클라이언트");
  console.log(`서버: ${BASE_URL}`);
  console.log("'help' 입력 시 명령 목록. 'g'로 게스트 빠른 시작.");

  let lastTicketId = "";

  mainLoop: while (true) {
    const cmd = (await rl.question("\nrtw> ")).trim().toLowerCase();
    try {
      switch (cmd) {
        case "":
          break;

        case "q":
        case "quit":
        case "exit":
          break mainLoop;

        case "help":
        case "h":
          console.log(HELP);
          break;

        case "state":
          printState(client);
          break;

        case "g": {
          const { guestGuid, session } = await client.guestQuickStart();
          show("게스트 입장 완료", { guestGuid, ...session });
          break;
        }

        case "signup": {
          const email = await str("이메일");
          const password = await str("비밀번호");
          await client.createAccount(email, password);
          show("계정 생성 완료", `${email} — 이제 login 하세요.`);
          break;
        }

        case "login": {
          const email = await str("이메일");
          const password = await str("비밀번호");
          await client.login(email, password);
          show("로그인 완료", "JWT 발급됨 — 이제 enter 로 게임 입장하세요.");
          break;
        }

        case "enter": {
          const session = await client.enterGame();
          show("게임 입장", session);
          break;
        }

        case "nick": {
          const nickname = await str("새 닉네임");
          const info = await client.updateNickname(nickname);
          show("내 정보 (닉네임 변경 결과)", info);
          break;
        }

        case "lobby": {
          show("로비", await client.getLobby());
          break;
        }

        case "savelobby": {
          const furnitureMasterId = await num("가구 마스터 ID", 2002);
          const posX = await num("PosX", 0);
          const posY = await num("PosY", 0);
          const rotation = await num("회전(0/90/180/270)", 0);
          const lobby = await client.saveLobby([{ furnitureMasterId, posX, posY, rotation }]);
          show("로비 저장 결과", lobby);
          break;
        }

        case "expand": {
          show("방 확장", await client.expandRoom());
          break;
        }

        case "gacha": {
          const gachaType = await num("가챠 타입", 0);
          const count = await num("횟수", 1);
          show("가챠 결과", await client.gacha(gachaType, count));
          break;
        }

        case "chars": {
          show("보유 캐릭터", await client.ownedCharacters());
          break;
        }

        case "mstart": {
          const missionId = await num("임무 ID", 101);
          const characterId = await num("캐릭터 ID", 1001);
          const ticket = await client.startMission(missionId, characterId);
          lastTicketId = ticket.ticketId;
          show("임무 시작 (티켓)", ticket);
          break;
        }

        case "mend": {
          const ticketId = await str("티켓 ID", lastTicketId);
          printMissionResult(await client.endMission(ticketId));
          break;
        }

        case "mission": {
          const missionId = await num("임무 ID", 101);
          const characterId = await num("캐릭터 ID", 1001);
          const ticket = await client.startMission(missionId, characterId);
          lastTicketId = ticket.ticketId;
          console.log(`티켓 발급: ${ticket.ticketId}`);
          printMissionResult(await client.endMission(ticket.ticketId));
          break;
        }

        case "logout": {
          await client.logout();
          show("로그아웃", "세션을 종료했습니다.");
          break;
        }

        case "raw": {
          const method = (await str("METHOD", "GET")).toUpperCase();
          const path = await str("PATH (예: /Lobby)");
          const bodyRaw = await str("JSON 바디 (없으면 Enter)");
          const body = bodyRaw === "" ? undefined : JSON.parse(bodyRaw);
          const auth = client.isSessionReady ? "session" : "none";
          show("raw 응답", await client.request(method, path, { body, auth }));
          break;
        }

        default:
          console.log(`알 수 없는 명령: '${cmd}'. 'help' 참고.`);
      }
    } catch (e) {
      printError(e);
    }
  }

  rl.close();
  console.log("종료합니다.");
}

main();

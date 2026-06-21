// 비대화형 엔드투엔드 스모크 테스트. 게스트 흐름으로 전체 API를 순차 호출하고,
// 각 단계의 성공/실패를 표시한 뒤 실패가 하나라도 있으면 종료코드 1로 끝낸다.
// 실행: node src/smoke.ts   (서버 주소: RTW_BASE_URL, 기본 http://localhost:5000)

import { env, exit } from "node:process";
import { RtwClient, ApiError } from "./api.ts";
import { missionOutcomeName } from "./types.ts";

const BASE_URL = env.RTW_BASE_URL ?? "http://localhost:5000";

let passed = 0;
let failed = 0;

async function step<T>(name: string, fn: () => Promise<T>): Promise<T | undefined> {
  try {
    const result = await fn();
    passed++;
    console.log(`✅ ${name}`);
    return result;
  } catch (e) {
    failed++;
    const msg = e instanceof ApiError ? e.message : (e as Error).message;
    console.log(`❌ ${name} — ${msg}`);
    return undefined;
  }
}

async function main(): Promise<void> {
  const client = new RtwClient(BASE_URL);
  console.log(`RTW 스모크 테스트 → ${BASE_URL}\n`);

  const guestGuid = await step("게스트 계정 생성", () => client.createGuestAccount());
  if (guestGuid !== undefined) {
    await step("게스트 로그인", () => client.guestLogin(guestGuid));
  }
  const session = await step("게임 입장 (enter)", () => client.enterGame());
  if (session !== undefined) {
    console.log(`   userId=${session.userId} nickname=${session.nickname}`);
  }

  await step("로비 조회", async () => {
    const lobby = await client.getLobby();
    console.log(`   roomGrade=${lobby.roomGrade} ${lobby.width}x${lobby.height} furniture=${lobby.furniture.length}`);
    return lobby;
  });

  await step("보유 캐릭터 조회", async () => {
    const chars = await client.ownedCharacters();
    console.log(`   ${chars.length}개: [${chars.map((c) => c.characterMasterId).join(", ")}]`);
    return chars;
  });

  await step("닉네임 변경 (내 정보 스냅샷)", async () => {
    const u = await client.updateNickname(`tester_${Date.now() % 100000}`);
    console.log(
      `   level=${u.level} stamina=${u.currentStamina}/${u.maxStamina} ` +
        `free=${u.freeCurrency} premium=${u.premiumCurrency} avatar=${u.mainCharacterId}`,
    );
    return u;
  });

  await step("가챠 1회 (재화 부족 시 실패 정상)", async () => {
    const g = await client.gacha(0, 1);
    console.log(
      `   뽑음=[${g.characterMasterIds.join(", ")}] free=${g.remainingFreeCurrency} premium=${g.remainingPremiumCurrency}`,
    );
    return g;
  });

  const ticket = await step("임무 시작 (start)", async () => {
    // 신규 게스트는 기본 캐릭터(1001)를 보유하므로 그 캐릭터로 투입한다.
    const t = await client.startMission(101, 1001);
    console.log(`   ticket=${t.ticketId} seed=${t.seed}`);
    return t;
  });

  if (ticket !== undefined) {
    await step("임무 정산 (end)", async () => {
      const r = await client.endMission(ticket.ticketId);
      console.log(
        `   ${missionOutcomeName(r.outcome)} fame+${r.fameGained} gold+${r.goldGained} → ` +
          `fame=${r.newFame} gold=${r.newGold} stamina=${r.newStamina}`,
      );
      return r;
    });
  }

  await step("방 확장 (expand)", async () => {
    const lobby = await client.expandRoom();
    console.log(`   roomGrade=${lobby.roomGrade} ${lobby.width}x${lobby.height}`);
    return lobby;
  });

  console.log(`\n결과: ${passed} 통과, ${failed} 실패`);
  exit(failed > 0 ? 1 : 0);
}

main();

// 브라우저 진입점. src/api.ts의 RtwClient SDK를 "그대로" 재사용해 UI에 연결한다.
// 이 파일은 개발 서버(web/serve.ts)가 타입을 벗겨 JS로 변환해 브라우저에 보낸다.
// erasable-only TS만 쓴다(enum/네임스페이스/파라미터 프로퍼티 금지).

import { RtwClient, ApiError } from "../src/api.ts";
import { missionOutcomeName, statKindName } from "../src/types.ts";
import type {
  UserInfo,
  LobbyInfo,
  LobbyFurniturePlacement,
  PlayerCharacterInfo,
  MissionTicketDto,
  MissionResultDto,
  CharacterGachaResult,
} from "../src/types.ts";

// baseUrl="" → 같은 오리진(개발 서버)으로 보내면 serve.ts가 백엔드로 프록시한다.
const client = new RtwClient("");

const $ = (id: string): HTMLElement => document.getElementById(id) as HTMLElement;
const val = (id: string): string => ($(id) as HTMLInputElement).value.trim();
const num = (id: string): number => Number(($(id) as HTMLInputElement).value);

// ── 활동 로그 ──
function log(msg: string, kind: "info" | "ok" | "err" | "dim" = "info"): void {
  const box = $("log");
  const time = new Date().toLocaleTimeString("ko-KR", { hour12: false });
  const line = document.createElement("div");
  line.innerHTML = `<span class="l-time">${time}</span> <span class="l-${kind}"></span>`;
  (line.lastElementChild as HTMLElement).textContent = msg;
  box.appendChild(line);
  box.scrollTop = box.scrollHeight;
}

// API 호출 공통 래퍼: 시작/성공/실패를 로그로 남긴다.
async function run<T>(label: string, fn: () => Promise<T>): Promise<T | undefined> {
  try {
    const out = await fn();
    log(`✓ ${label}`, "ok");
    return out;
  } catch (e) {
    const msg = e instanceof ApiError ? e.message : (e as Error).message;
    log(`✗ ${label} — ${msg}`, "err");
    return undefined;
  }
}

// ── 세션 표시 ──
function renderSession(): void {
  const ready = client.isSessionReady;
  $("s-state").textContent = ready ? "접속됨" : "미접속";
  $("s-uid").textContent = client.userId !== null ? String(client.userId) : "—";
  $("s-nick").textContent = client.nickname ?? "—";
  $("s-token").textContent = client.token !== null ? client.token.slice(0, 12) + "…" : "—";
  ($("btn-logout") as HTMLButtonElement).disabled = !ready;
}

// ── 내 정보(스냅샷) 표시 ──
function renderStats(u: UserInfo): void {
  $("st-level").textContent = String(u.level);
  $("st-premium").textContent = String(u.premiumCurrency);
  $("st-free").textContent = String(u.freeCurrency);
  $("st-exp").textContent = String(u.currentExp);
  $("st-main").textContent = String(u.mainCharacterId);
  $("st-stamina").textContent = `${u.currentStamina} / ${u.maxStamina}`;
  const pct = u.maxStamina > 0 ? Math.round((u.currentStamina / u.maxStamina) * 100) : 0;
  ($("st-stamina-bar") as HTMLElement).style.width = `${pct}%`;
}

// ════════ 로비 ════════
let lobby: LobbyInfo | null = null;
const placements = new Map<string, LobbyFurniturePlacement>();
const key = (x: number, y: number): string => `${x},${y}`;

function renderLobby(): void {
  if (lobby === null) return;
  $("lobby-meta").textContent = `등급 ${lobby.roomGrade} · ${lobby.width}×${lobby.height} · 가구 ${placements.size}개`;

  const grid = $("lobby-grid");
  grid.innerHTML = "";
  const cell = Math.max(8, Math.min(16, Math.floor(560 / lobby.width)));
  grid.style.gridTemplateColumns = `repeat(${lobby.width}, ${cell}px)`;
  grid.style.setProperty("--cell", `${cell}px`);

  for (let y = 0; y < lobby.height; y++) {
    for (let x = 0; x < lobby.width; x++) {
      const div = document.createElement("div");
      div.className = "cell";
      const p = placements.get(key(x, y));
      if (p !== undefined) {
        div.classList.add("occupied");
        div.textContent = String(p.furnitureMasterId);
        div.title = `(${x},${y}) 가구 ${p.furnitureMasterId}`;
      }
      div.addEventListener("click", () => toggleCell(x, y));
      grid.appendChild(div);
    }
  }
}

function toggleCell(x: number, y: number): void {
  const k = key(x, y);
  if (placements.has(k)) placements.delete(k);
  else placements.set(k, { furnitureMasterId: num("inp-furniture"), posX: x, posY: y, rotation: 0 });
  renderLobby();
}

function loadLobby(info: LobbyInfo): void {
  lobby = info;
  placements.clear();
  for (const f of info.furniture) {
    placements.set(key(f.posX, f.posY), {
      furnitureMasterId: f.furnitureMasterId,
      posX: f.posX,
      posY: f.posY,
      rotation: f.rotation,
    });
  }
  renderLobby();
}

// ════════ 가챠 / 캐릭터 ════════
function renderGacha(g: CharacterGachaResult): void {
  const box = $("gacha-result");
  box.innerHTML = "";
  for (const id of g.characterMasterIds) {
    const c = document.createElement("span");
    c.className = "chip new";
    c.textContent = `🎉 ${id}`;
    box.appendChild(c);
  }
  const bal = document.createElement("span");
  bal.className = "chip";
  bal.textContent = `잔액 프리미엄 ${g.remainingPremiumCurrency} · 프리 ${g.remainingFreeCurrency}`;
  box.appendChild(bal);
  $("st-premium").textContent = String(g.remainingPremiumCurrency);
  $("st-free").textContent = String(g.remainingFreeCurrency);
}

function renderChars(chars: PlayerCharacterInfo[]): void {
  const box = $("chars-result");
  box.innerHTML = "";
  if (chars.length === 0) {
    box.innerHTML = `<span class="muted">보유 캐릭터 없음</span>`;
    return;
  }
  for (const c of chars) {
    const chip = document.createElement("span");
    chip.className = "chip";
    chip.textContent = `#${c.characterMasterId} (Lv ${c.level})`;
    box.appendChild(chip);
  }
}

// ════════ 임무 ════════
let ticket: MissionTicketDto | null = null;

function renderMissionResult(r: MissionResultDto): void {
  const win = r.outcome === 0;
  const head = $("mission-head");
  head.innerHTML =
    `<span class="outcome ${win ? "win" : "lose"}">${missionOutcomeName(r.outcome)}</span> ` +
    `· 명성 +${r.fameGained} → ${r.newFame} · 골드 +${r.goldGained} → ${r.newGold} · 스태미나 ${r.newStamina}`;

  const box = $("mission-log");
  box.innerHTML = "";
  r.log.forEach((e, i) => {
    const line = document.createElement("div");
    line.className = `log-line ${e.passed ? "pass" : "fail"}`;
    line.style.animationDelay = `${i * 110}ms`;
    line.innerHTML =
      `<span class="idx">${e.index}</span>` +
      `<span></span>` +
      `<span class="roll">${statKindName(e.stat)} <b>${e.roll}</b>/${e.required} ${e.passed ? "✓" : "✗"}</span>`;
    (line.children[1] as HTMLElement).textContent = `[${e.stage}] ${e.message} (멘탈 ${e.mentalAfter})`;
    box.appendChild(line);
  });
  $("st-stamina").textContent = `${r.newStamina} / —`;
}

// ════════ 이벤트 바인딩 ════════
function afterEnter(): void {
  renderSession();
  log(`게임 입장 완료: userId=${client.userId}, nickname=${client.nickname}`, "info");
  run("로비 조회", () => client.getLobby()).then((l) => l && loadLobby(l));
  run("보유 캐릭터 조회", () => client.ownedCharacters()).then((c) => c && renderChars(c));
}

function bind(): void {
  $("btn-guest").addEventListener("click", async () => {
    const r = await run("게스트 빠른 시작 (생성→로그인→입장)", () => client.guestQuickStart());
    if (r) afterEnter();
  });

  $("btn-signup").addEventListener("click", async () => {
    const email = val("inp-email");
    const pass = val("inp-pass");
    if (email === "" || pass === "") return log("이메일/비밀번호를 입력하세요.", "err");
    await run("계정 생성", () => client.createAccount(email, pass));
    const jwt = await run("로그인", () => client.login(email, pass));
    if (jwt === undefined) return;
    const s = await run("게임 입장", () => client.enterGame());
    if (s) afterEnter();
  });

  $("btn-login").addEventListener("click", async () => {
    const email = val("inp-email");
    const pass = val("inp-pass");
    if (email === "" || pass === "") return log("이메일/비밀번호를 입력하세요.", "err");
    const jwt = await run("로그인", () => client.login(email, pass));
    if (jwt === undefined) return;
    const s = await run("게임 입장", () => client.enterGame());
    if (s) afterEnter();
  });

  $("btn-logout").addEventListener("click", async () => {
    await run("로그아웃", () => client.logout());
    renderSession();
  });

  $("btn-nick").addEventListener("click", async () => {
    const nick = val("inp-nick") || `tester_${Date.now() % 100000}`;
    const u = await run("닉네임 변경 / 정보 갱신", () => client.updateNickname(nick));
    if (u) {
      renderStats(u);
      renderSession();
    }
  });

  $("btn-lobby-refresh").addEventListener("click", async () => {
    const l = await run("로비 조회", () => client.getLobby());
    if (l) loadLobby(l);
  });

  $("btn-expand").addEventListener("click", async () => {
    const l = await run("방 확장", () => client.expandRoom());
    if (l) loadLobby(l);
  });

  $("btn-lobby-save").addEventListener("click", async () => {
    const items = [...placements.values()];
    const l = await run(`로비 저장 (가구 ${items.length}개)`, () => client.saveLobby(items));
    if (l) loadLobby(l);
  });

  $("btn-gacha").addEventListener("click", async () => {
    const g = await run("가챠", () => client.gacha(num("inp-gtype"), num("inp-gcount")));
    if (g) renderGacha(g);
  });

  $("btn-chars").addEventListener("click", async () => {
    const c = await run("보유 캐릭터 조회", () => client.ownedCharacters());
    if (c) renderChars(c);
  });

  $("btn-mstart").addEventListener("click", async () => {
    const t = await run("임무 시작", () => client.startMission(num("inp-mission"), num("inp-char")));
    if (t) {
      ticket = t;
      $("mission-head").innerHTML = `<span class="muted">티켓 발급됨: ${t.ticketId} (seed ${t.seed}) — 정산을 누르세요.</span>`;
      ($("btn-mend") as HTMLButtonElement).disabled = false;
    }
  });

  $("btn-mend").addEventListener("click", async () => {
    if (ticket === null) return log("먼저 임무를 시작하세요.", "err");
    const r = await run("임무 정산", () => client.endMission(ticket!.ticketId));
    if (r) {
      renderMissionResult(r);
      ticket = null;
      ($("btn-mend") as HTMLButtonElement).disabled = true;
    }
  });

  $("btn-mauto").addEventListener("click", async () => {
    const t = await run("임무 시작", () => client.startMission(num("inp-mission"), num("inp-char")));
    if (!t) return;
    const r = await run("임무 정산", () => client.endMission(t.ticketId));
    if (r) renderMissionResult(r);
  });
}

// ── 백엔드 헬스 체크 ──
async function checkHealth(): Promise<void> {
  try {
    const res = await fetch("/health", { method: "GET" });
    if (res.ok) {
      $("conn-dot").className = "dot ok";
      $("conn-text").textContent = "백엔드 연결됨";
      return;
    }
    throw new Error(`HTTP ${res.status}`);
  } catch {
    $("conn-dot").className = "dot err";
    $("conn-text").textContent = "백엔드 응답 없음 — 서버를 켜세요";
  }
}

bind();
renderSession();
checkHealth();
log("준비 완료. ‘게스트로 빠른 시작’으로 시작하세요.", "dim");

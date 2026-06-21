// RTWWebServer REST API의 타입드 클라이언트 SDK.
// 의존성 0 — Node 내장 fetch만 사용한다. 브라우저 UI를 붙일 때도 이 파일을 그대로 재사용할 수 있다.

import { WebServerErrorCode, errorCodeName } from "./types.ts";
import type {
  UserSession,
  UserInfo,
  MissionTicketDto,
  MissionResultDto,
  LobbyInfo,
  CharacterGachaResult,
  PlayerCharacterInfo,
  LobbyFurniturePlacement,
} from "./types.ts";

export class ApiError extends Error {
  httpStatus: number;
  errorCode: number | null;

  constructor(message: string, httpStatus: number, errorCode: number | null) {
    super(message);
    this.name = "ApiError";
    this.httpStatus = httpStatus;
    this.errorCode = errorCode;
  }
}

type AuthMode = "none" | "jwt" | "session";

interface RequestOptions {
  body?: unknown;
  auth?: AuthMode;
}

export class RtwClient {
  baseUrl: string;
  jwt: string | null = null;
  userId: number | null = null;
  token: string | null = null;
  nickname: string | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/+$/, "");
  }

  get isSessionReady(): boolean {
    return this.userId !== null && this.token !== null;
  }

  clearSession(): void {
    this.userId = null;
    this.token = null;
    this.nickname = null;
  }

  // 모든 호출의 공통 경로: 헤더 구성 → 요청 → GameResponse 봉투 해석 → 에러는 ApiError로 던진다.
  async request<T>(method: string, path: string, opts: RequestOptions = {}): Promise<T> {
    const auth = opts.auth ?? "session";
    const headers: Record<string, string> = { Accept: "application/json" };
    if (opts.body !== undefined) {
      headers["Content-Type"] = "application/json";
    }

    if (auth === "jwt") {
      if (this.jwt === null) {
        throw new ApiError("JWT가 없습니다. 먼저 login 또는 guestLogin 하세요.", 0, null);
      }
      headers.Authorization = `Bearer ${this.jwt}`;
    } else if (auth === "session") {
      if (!this.isSessionReady) {
        throw new ApiError("세션이 없습니다. 먼저 enter(게임 입장) 하세요.", 0, null);
      }
      headers["X-User-Id"] = String(this.userId);
      headers["X-Auth-Token"] = this.token as string;
    }

    let res: Response;
    try {
      res = await fetch(`${this.baseUrl}${path}`, {
        method,
        headers,
        body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
      });
    } catch (e) {
      throw new ApiError(
        `서버 연결 실패 (${this.baseUrl}). 웹서버가 실행 중인지 확인하세요. (${(e as Error).message})`,
        0,
        null,
      );
    }

    const text = await res.text();
    let json: any = null;
    if (text.length > 0) {
      try {
        json = JSON.parse(text);
      } catch {
        // JSON이 아닌 응답(드묾) — 아래에서 원문으로 처리
      }
    }

    // 정상 경로: GameResponse 봉투 { errorCode, data? }
    if (json !== null && typeof json.errorCode === "number") {
      if (json.errorCode !== WebServerErrorCode.Success) {
        throw new ApiError(`${errorCodeName(json.errorCode)} (${json.errorCode})`, res.status, json.errorCode);
      }
      return json.data as T;
    }

    // GameResponse가 아님: [ApiController] 모델검증 실패(ProblemDetails) 등
    if (!res.ok) {
      const detail = json !== null ? JSON.stringify(json) : text || res.statusText;
      throw new ApiError(`HTTP ${res.status}: ${detail}`, res.status, null);
    }

    return json as T;
  }

  // ── 계정 / 로그인 (인증 불필요) ──
  createGuestAccount(): Promise<string> {
    return this.request<string>("POST", "/Account/createGuestAccount", { auth: "none" });
  }

  createAccount(email: string, password: string): Promise<void> {
    return this.request<void>("POST", "/Account/createAccount", { auth: "none", body: { email, password } });
  }

  async login(email: string, password: string): Promise<string> {
    const jwt = await this.request<string>("POST", "/Login/login", { auth: "none", body: { email, password } });
    this.jwt = jwt;
    return jwt;
  }

  async guestLogin(guestGuid: string): Promise<string> {
    const jwt = await this.request<string>("POST", "/Login/guestLogin", { auth: "none", body: { guestGuid } });
    this.jwt = jwt;
    return jwt;
  }

  // ── 게임 입장 (JWT Bearer) → 세션 토큰 발급 ──
  async enterGame(): Promise<UserSession> {
    const session = await this.request<UserSession>("POST", "/Game/enter", { auth: "jwt" });
    this.userId = session.userId;
    this.token = session.token;
    this.nickname = session.nickname;
    return session;
  }

  // 편의: 게스트 계정 생성 → 로그인 → 입장을 한 번에.
  async guestQuickStart(): Promise<{ guestGuid: string; session: UserSession }> {
    const guestGuid = await this.createGuestAccount();
    await this.guestLogin(guestGuid);
    const session = await this.enterGame();
    return { guestGuid, session };
  }

  // ── 유저 (세션 인증) ──
  async updateNickname(nickname: string): Promise<UserInfo> {
    const info = await this.request<UserInfo>("POST", "/User/nickname", { body: { nickname } });
    this.nickname = info.nickname;
    return info;
  }

  async logout(): Promise<void> {
    await this.request<void>("POST", "/User/logout");
    this.clearSession();
  }

  // ── 캐릭터 ──
  gacha(gachaType: number, count: number): Promise<CharacterGachaResult> {
    return this.request<CharacterGachaResult>("POST", "/Character/gacha", { body: { gachaType, count } });
  }

  ownedCharacters(): Promise<PlayerCharacterInfo[]> {
    return this.request<PlayerCharacterInfo[]>("GET", "/Character/owned");
  }

  // ── 로비 ──
  getLobby(): Promise<LobbyInfo> {
    return this.request<LobbyInfo>("GET", "/Lobby");
  }

  saveLobby(items: LobbyFurniturePlacement[]): Promise<LobbyInfo> {
    return this.request<LobbyInfo>("POST", "/Lobby", { body: { items } });
  }

  expandRoom(): Promise<LobbyInfo> {
    return this.request<LobbyInfo>("POST", "/Lobby/expand");
  }

  // ── 임무 ──
  // characterId = 투입할 보유 캐릭터의 마스터 ID. 서버가 소유 여부를 검증한다.
  startMission(missionId: number, characterId: number): Promise<MissionTicketDto> {
    return this.request<MissionTicketDto>("POST", "/Mission/start", { body: { missionId, characterId } });
  }

  endMission(ticketId: string): Promise<MissionResultDto> {
    return this.request<MissionResultDto>("POST", "/Mission/end", { body: { ticketId } });
  }
}

// RTWWebServer의 C# DTO/enum을 미러링한다.
// 응답 JSON은 ASP.NET 기본(Web) 직렬화라 camelCase다. 요청도 camelCase로 보낸다(서버는 대소문자 무시 바인딩).
//
// 주의: C#의 long(예: ticket seed)은 2^53을 넘을 수 있어 JS number로 파싱하면 정밀도가 깨질 수 있다.
// seed는 표시용(클라가 서버로 다시 보내지 않음)이라 실동작에는 영향이 없다.

// NetworkDefinition/ErrorCode/WebServerErrorCode.cs 와 동기화
export const WebServerErrorCode = {
  Success: 1000,
  InvalidEmail: 1001,
  InvalidPassword: 1002,
  AccountNotFound: 1003,
  GuestNotFound: 1004,
  InvalidRequestHttpBody: 1005,
  InvalidAuthToken: 1006,
  InsufficientCurrency: 1007,
  InvalidArgument: 1008,
  UserNotFound: 1009,
  DuplicateNickname: 1010,
  DuplicateEmail: 1011,
  InvalidCredentials: 1012,
  DuplicateCharacter: 1013,
  MissionNotFound: 1014,
  InsufficientStamina: 1015,
  MissionResultNotReady: 1016,
  MissionTicketNotFound: 1017,
  DatabaseError: 1100,
  RemoteCacheError: 1200,
  RemoteCacheLockFailed: 1201,
  InternalServerError: 1999,
} as const;

const errorCodeNames: Record<number, string> = Object.fromEntries(
  Object.entries(WebServerErrorCode).map(([name, code]) => [code, name]),
);

export function errorCodeName(code: number): string {
  return errorCodeNames[code] ?? `Unknown(${code})`;
}

// RTWWebServer/Game/Mission/MissionOutcome.cs
export const MissionOutcome = { Win: 0, Lose: 1 } as const;
export function missionOutcomeName(value: number): string {
  return value === 0 ? "Win" : value === 1 ? "Lose" : String(value);
}

// RTWWebServer/Game/Mission/StatKind.cs
export const StatKind = { Portfolio: 0, Development: 1, JobSearching: 2 } as const;
export function statKindName(value: number): string {
  return ["Portfolio", "Development", "JobSearching"][value] ?? String(value);
}

// 모든 컨트롤러 응답의 공통 봉투(GameResponse / GameResponse<T>)
export interface GameResponse<T> {
  errorCode: number;
  data?: T;
}

export interface UserSession {
  userId: number;
  token: string;
  nickname: string;
}

export interface UserInfo {
  id: number;
  nickname: string;
  level: number;
  currentExp: number;
  currentStamina: number;
  maxStamina: number;
  premiumCurrency: number;
  freeCurrency: number;
  mainCharacterId: number; // 프로필 대표 캐릭터(아바타). 임무 투입 캐릭터와는 별개.
}

export interface MissionTicketDto {
  ticketId: string;
  seed: number;
}

export interface BattleLogEntryDto {
  index: number;
  stage: string;
  stat: number;
  roll: number;
  required: number;
  passed: boolean;
  mentalAfter: number;
  message: string;
}

export interface MissionResultDto {
  outcome: number;
  log: BattleLogEntryDto[];
  fameGained: number;
  goldGained: number;
  newFame: number;
  newGold: number;
  newStamina: number;
  seed: number;
}

export interface LobbyFurnitureInfo {
  id: number;
  furnitureMasterId: number;
  posX: number;
  posY: number;
  rotation: number;
  updatedAt: string;
}

export interface LobbyInfo {
  roomGrade: number;
  width: number;
  height: number;
  furniture: LobbyFurnitureInfo[];
}

export interface CharacterGachaResult {
  characterMasterIds: number[];
  remainingPremiumCurrency: number;
  remainingFreeCurrency: number;
}

export interface PlayerCharacterInfo {
  id: number;
  characterMasterId: number;
  level: number;
  currentExp: number;
  obtainedAt: string;
  updatedAt: string;
}

export interface LobbyFurniturePlacement {
  furnitureMasterId: number;
  posX: number;
  posY: number;
  rotation: number;
}

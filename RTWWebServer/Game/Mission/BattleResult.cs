namespace RTWWebServer.Game.Mission;

// 시뮬레이션 1회의 전체 결과. Seed를 포함해 동일 입력으로 로그를 재현할 수 있게 한다.
public sealed record BattleResult(
    MissionOutcome Outcome,
    IReadOnlyList<BattleLogEntry> Log,
    long Seed);

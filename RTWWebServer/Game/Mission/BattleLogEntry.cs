namespace RTWWebServer.Game.Mission;

// 전투 한 스테이지의 판정 결과. 결정론 시뮬레이션이 만들어 내는 불변 이벤트.
public sealed record BattleLogEntry(
    int Index,
    string Stage,
    StatKind Stat,
    int Roll,
    int Required,
    bool Passed,
    int MentalAfter,
    string Message);

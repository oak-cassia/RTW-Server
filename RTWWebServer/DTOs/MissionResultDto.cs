using RTWWebServer.Game.Mission;

namespace RTWWebServer.DTOs;

public class MissionResultDto
{
    public MissionOutcome Outcome { get; set; }
    public BattleLogEntryDto[] Log { get; set; } = [];

    // 보상은 D6에 따라 승리 시에만 지급된다. 패배 시 0.
    public long FameGained { get; set; }
    public long GoldGained { get; set; }
    public long ExpGained { get; set; }

    // 보상 반영 후 갱신된 잔액/상태(클라이언트가 재조회 없이 즉시 표시).
    public long NewFame { get; set; }
    public long NewGold { get; set; }
    public long NewExp { get; set; }
    public int NewStamina { get; set; }

    // 결정론 시뮬레이션의 시드. 동일 시드 + 동일 입력 → 동일 로그(재현/검증용).
    public long Seed { get; set; }
}

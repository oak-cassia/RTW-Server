using RTWWebServer.Game.Mission;

namespace RTWWebServer.DTOs;

// 전투 결과의 Redis 표현(서버 간 계약). 실제로는 게임서버가 전투 종료 시 이 형태로 기록하고,
// end가 읽어 정산한다. 스켈레톤에선 start의 스텁 시뮬레이터가 대신 기록한다.
// 게임서버 도입 시 이 계약은 NetworkDefinition으로 옮겨 두 서버가 공유하는 게 바람직하다.
public class MissionRunResult
{
    public MissionOutcome Outcome { get; set; }
    public BattleLogEntryDto[] Log { get; set; } = [];
    public long Seed { get; set; }
}

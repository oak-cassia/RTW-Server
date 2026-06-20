using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.Game.Mission;

// 스텁 구현 — 서버 골격(배선) 검증용. 실제 전투 판정은 아직 비어 있다.
// TODO(세부 기획 반영): mission.Stages를 순회하며 (해당 스탯 + seed 기반 난수) vs RequiredScore로
// 통과/실패를 판정하고, 실패마다 멘탈을 MentalPenalty만큼 깎아 0 이하면 Lose로 종료하도록 교체한다.
// 참고: docs/mission-lobby-design.md §4.3
public sealed class MissionBattleSimulator : IMissionBattleSimulator
{
    public BattleResult Simulate(CharacterMaster character, MissionMaster mission, long seed)
    {
        // 현재는 항상 승리 + 로그 1줄. 흐름이 전 계층을 관통하는지 확인하기 위한 placeholder.
        var log = new List<BattleLogEntry>
        {
            new(
                Index: 0,
                Stage: mission.Name,
                Stat: StatKind.Portfolio,
                Roll: 0,
                Required: 0,
                Passed: true,
                MentalAfter: mission.StartingMental,
                Message: $"[스텁] {character.Name}이(가) 임무 '{mission.Name}'을(를) 자동 통과")
        };

        return new BattleResult(MissionOutcome.Win, log, seed);
    }
}

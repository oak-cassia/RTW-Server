using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.Game.Mission;

// 결정론 전투 시뮬레이터. 같은 (캐릭터, 임무, seed)면 항상 같은 로그/결과를 만든다(클라가 결과를 위조 못 함).
// 스테이지마다 (스탯 + seed 기반 난수) vs RequiredScore로 통과/실패를 판정하고, 실패 시 멘탈을 깎아
// 0 이하가 되면 즉시 탈락(Lose)한다. 규칙: docs/mission-lobby-design.md §4.
public sealed class MissionBattleSimulator : IMissionBattleSimulator
{
    private const int MaxVariance = 20;

    public BattleResult Simulate(CharacterMaster character, MissionMaster mission, long seed)
    {
        var rng = new Random(unchecked((int)seed)); // seed로만 굴림, 재현 가능
        var log = new List<BattleLogEntry>(mission.Stages.Length);
        int mental = mission.StartingMental;
        var outcome = MissionOutcome.Win;

        foreach ((int i, MissionStage stage) in mission.Stages.Index())
        {
            var statValue = stage.Stat;
            int stat = statValue switch
            {

                StatKind.Portfolio => character.Portfolio,
                StatKind.Development => character.Development,
                StatKind.JobSearching => character.JobSearching,
                _ => throw new ArgumentOutOfRangeException(nameof(statValue), statValue, "Unknown stat kind")
            };

            int roll = stat + rng.Next(0, MaxVariance + 1); // Next의 max는 미포함 → 0..MaxVariance(포함)
            bool passed = roll >= stage.RequiredScore;

            // 실패한 스테이지만 멘탈 차감s
            if (!passed)
            {
                mental -= stage.MentalPenalty;
            }

            log.Add(new BattleLogEntry(
                i,
                stage.FlavorText,
                statValue,
                roll,
                stage.RequiredScore,
                passed,
                mental,
                $"[{stage.FlavorText}] {stage.Stat} {stat} → 굴림 {roll} vs 요구 {stage.RequiredScore} → {(passed ? "통과" : "실패")} (멘탈 {mental})"
            ));

            if (mental <= 0) // 0 이하 → 즉시 탈락
            {
                outcome = MissionOutcome.Lose;
                break;
            }
        }

        return new BattleResult(outcome, log, seed);
    }
}
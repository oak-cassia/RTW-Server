using RTWWebServer.Game.Mission;
using RTWWebServer.MasterDatas.Models;

namespace RTWTest.WebServer.Game.Mission;

// 결정론 전투 시뮬레이터의 판정 규칙을 경계값으로 고정한다.
[TestFixture]
public class MissionBattleSimulatorTests
{
    private static readonly MissionBattleSimulator Simulator = new();

    private static CharacterMaster Character(int portfolio = 50, int development = 50, int jobSearching = 50) =>
        new() { Id = 1, Name = "유우", Portfolio = portfolio, Development = development, JobSearching = jobSearching };

    private static MissionMaster Mission(int startingMental, params MissionStage[] stages) =>
        new()
        {
            Id = 101, Name = "테스트 임무", StaminaCost = 5,
            StartingMental = startingMental, RewardFame = 120, RewardGold = 500, Stages = stages
        };

    private static MissionStage Stage(StatKind stat, int required, int penalty, string flavor = "관문") =>
        new() { Stat = stat, RequiredScore = required, MentalPenalty = penalty, FlavorText = flavor };

    // 모든 스테이지를 확실히 통과(스탯 >= 요구치 → 굴림 +0이라도 통과). 멘탈은 그대로, Win.
    [Test]
    public void AllStagesPassed_ReturnsWin_MentalUnchanged()
    {
        var character = Character(portfolio: 30, development: 30, jobSearching: 30);
        var mission = Mission(100,
            Stage(StatKind.Portfolio, required: 10, penalty: 40),
            Stage(StatKind.Development, required: 10, penalty: 40),
            Stage(StatKind.JobSearching, required: 10, penalty: 40));

        var result = Simulator.Simulate(character, mission, seed: 1);

        Assert.That(result.Outcome, Is.EqualTo(MissionOutcome.Win));
        Assert.That(result.Log, Has.Count.EqualTo(3));
        Assert.That(result.Log, Has.All.Property("Passed").True);
        Assert.That(result.Log[^1].MentalAfter, Is.EqualTo(100));
    }

    // 멘탈이 정확히 0이 되는 순간 즉시 탈락하고 그 스테이지에서 멈춘다(이후 스테이지 미평가).
    [Test]
    public void MentalHitsZero_ReturnsLose_StopsImmediately()
    {
        var character = Character(0, 0, 0); // 강제 실패: 최대 굴림(0+20)도 요구치 미달
        var mission = Mission(startingMental: 60,
            Stage(StatKind.Portfolio, required: 1000, penalty: 30), // 60 → 30
            Stage(StatKind.Development, required: 1000, penalty: 30), // 30 → 0 → 탈락
            Stage(StatKind.JobSearching, required: 1000, penalty: 30)); // 도달 안 함

        var result = Simulator.Simulate(character, mission, seed: 1);

        Assert.That(result.Outcome, Is.EqualTo(MissionOutcome.Lose));
        Assert.That(result.Log, Has.Count.EqualTo(2));
        Assert.That(result.Log[^1].MentalAfter, Is.EqualTo(0));
    }

    // 실패해도 멘탈이 남으면(0 직전) 다음 스테이지로 진행하고, 끝까지 살아남으면 Win.
    [Test]
    public void FailedStageButMentalSurvives_ContinuesAndCanWin()
    {
        var character = Character(portfolio: 0, development: 30, jobSearching: 30);
        var mission = Mission(startingMental: 50,
            Stage(StatKind.Portfolio, required: 1000, penalty: 30), // 실패 50 → 20 (생존)
            Stage(StatKind.Development, required: 10, penalty: 40), // 통과
            Stage(StatKind.JobSearching, required: 10, penalty: 40)); // 통과

        var result = Simulator.Simulate(character, mission, seed: 1);

        Assert.That(result.Outcome, Is.EqualTo(MissionOutcome.Win));
        Assert.That(result.Log, Has.Count.EqualTo(3));
        Assert.That(result.Log[0].Passed, Is.False);
        Assert.That(result.Log[0].MentalAfter, Is.EqualTo(20));
        Assert.That(result.Log[1].Passed, Is.True);
        Assert.That(result.Log[2].Passed, Is.True);
    }

    // 같은 seed면 로그가 완전히 동일하다(결정론). 난수가 통과/실패를 가르도록 요구치를 굴림 범위 안에 둔다.
    [Test]
    public void SameSeed_ProducesIdenticalLog()
    {
        var character = Character(20, 20, 20); // 굴림 범위 20..40
        var mission = Mission(startingMental: 100,
            Stage(StatKind.Portfolio, required: 30, penalty: 30),
            Stage(StatKind.Development, required: 30, penalty: 30),
            Stage(StatKind.JobSearching, required: 30, penalty: 30));

        var a = Simulator.Simulate(character, mission, seed: 12345);
        var b = Simulator.Simulate(character, mission, seed: 12345);

        Assert.That(a.Outcome, Is.EqualTo(b.Outcome));
        Assert.That(a.Log, Is.EqualTo(b.Log)); // BattleLogEntry는 record라 값 동등성으로 비교
    }

    // 굴림은 스탯 + 0..MaxVariance(20 포함). 스탯 0으로 두고 고정 seed들을 굴려 0과 20에 모두 도달함을 확인
    // → MaxVariance off-by-one(0..18, 0..19 등) 회귀 방지. seed가 고정이라 결과는 결정론적이다.
    [Test]
    public void Roll_CoversFullVarianceRange_Inclusive()
    {
        var character = Character(0, 0, 0);
        var mission = Mission(startingMental: 1000, // 절대 탈락 안 함 → 로그가 항상 남음
            Stage(StatKind.Portfolio, required: 1000, penalty: 1));

        int minRoll = int.MaxValue;
        int maxRoll = int.MinValue;
        for (long seed = 0; seed < 500; seed++)
        {
            int roll = Simulator.Simulate(character, mission, seed).Log[0].Roll;
            minRoll = Math.Min(minRoll, roll);
            maxRoll = Math.Max(maxRoll, roll);
        }

        Assert.That(minRoll, Is.EqualTo(0));
        Assert.That(maxRoll, Is.EqualTo(20)); // 0..20 포함
    }
}

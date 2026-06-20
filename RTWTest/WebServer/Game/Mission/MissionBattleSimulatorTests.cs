using RTWWebServer.Game.Mission;
using RTWWebServer.MasterDatas.Models;

namespace RTWTest.WebServer.Game.Mission;

// 스텁 시뮬레이터의 계약 가드. 실제 전투 판정으로 교체될 때 이 테스트가 회귀 기준이 된다.
[TestFixture]
public class MissionBattleSimulatorTests
{
    [Test]
    public void Simulate_ReturnsWin_WithNonEmptyLog_AndEchoesSeed()
    {
        var simulator = new MissionBattleSimulator();
        var character = new CharacterMaster { Id = 1, Name = "유우", Portfolio = 15, Development = 25, JobSearching = 30 };
        var mission = new MissionMaster { Id = 101, Name = "테스트 임무", StaminaCost = 5, StartingMental = 100, RewardFame = 120, RewardGold = 500 };

        const long seed = 42;
        var result = simulator.Simulate(character, mission, seed);

        Assert.That(result.Outcome, Is.EqualTo(MissionOutcome.Win));
        Assert.That(result.Log, Is.Not.Empty);
        Assert.That(result.Seed, Is.EqualTo(seed));
    }
}

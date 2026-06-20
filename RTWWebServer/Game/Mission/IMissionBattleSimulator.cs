using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.Game.Mission;

// 임무 전투의 결정론적 시뮬레이터. 순수 함수(DB·시간·전역상태 무관)로 두어 단위테스트가 쉽고,
// 세부 전투 판정은 이 경계 안에서만 바뀌도록 격리한다.
public interface IMissionBattleSimulator
{
    BattleResult Simulate(CharacterMaster character, MissionMaster mission, long seed);
}

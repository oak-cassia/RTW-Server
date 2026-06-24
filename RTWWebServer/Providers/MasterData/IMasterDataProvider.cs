using System.Collections.Immutable;
using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.Providers.MasterData;

public interface IMasterDataProvider
{
    bool TryGetCharacter(int id, out CharacterMaster character);
    ImmutableDictionary<int, CharacterMaster> GetAllCharacters();
    bool TryGetFurniture(int id, out FurnitureMaster furniture);
    ImmutableDictionary<int, FurnitureMaster> GetAllFurniture();
    bool TryGetRoomGrade(int grade, out RoomGradeMaster roomGrade);
    ImmutableDictionary<int, RoomGradeMaster> GetAllRoomGrades();
    bool TryGetMission(int id, out MissionMaster mission);
    ImmutableDictionary<int, MissionMaster> GetAllMissions();
    ImmutableDictionary<int, RankMaster> GetAllRanks();

    // 누적 명성으로 현재 랭크를 파생한다(저장하지 않음). RequiredFame이 fame 이하인 랭크 중 가장 높은 Rank.
    // baseline(RequiredFame 0)이 있으면 항상 1 이상, 임계값 어디에도 못 미치면 0(랭크 없음).
    int GetRankByFame(long fame);
}
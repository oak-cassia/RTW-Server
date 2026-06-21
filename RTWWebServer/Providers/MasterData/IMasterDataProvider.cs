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
}
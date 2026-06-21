using System.Collections.Immutable;
using RTWWebServer.MasterDatas;
using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.Providers.MasterData;

public sealed class MasterDataProvider : IMasterDataProvider
{
    private readonly ImmutableDictionary<int, CharacterMaster> _characters;
    private readonly ImmutableDictionary<int, FurnitureMaster> _furniture;
    private readonly ImmutableDictionary<int, RoomGradeMaster> _roomGrades;
    private readonly ImmutableDictionary<int, MissionMaster> _missions;

    public MasterDataProvider(IMasterDataLoader loader, ILogger<MasterDataProvider> logger)
    {
        // 시작 시 1회 로드한다. 잘못된 데이터/누락 파일은 로더가 예외를 던져 기동을 막는다(fail-fast).
        var set = loader.Load();
        _characters = set.Characters;
        _furniture = set.Furniture;
        _roomGrades = set.RoomGrades;
        _missions = set.Missions;

        logger.LogInformation(
            "Master data loaded. Characters count: {CharacterCount}, Furniture count: {FurnitureCount}, RoomGrades count: {RoomGradeCount}, Missions count: {MissionCount}",
            _characters.Count, _furniture.Count, _roomGrades.Count, _missions.Count);
    }

    public bool TryGetCharacter(int id, out CharacterMaster character)
        => _characters.TryGetValue(id, out character!);

    public ImmutableDictionary<int, CharacterMaster> GetAllCharacters()
        => _characters;

    public bool TryGetFurniture(int id, out FurnitureMaster furniture)
        => _furniture.TryGetValue(id, out furniture!);

    public ImmutableDictionary<int, FurnitureMaster> GetAllFurniture()
        => _furniture;

    public bool TryGetRoomGrade(int grade, out RoomGradeMaster roomGrade)
        => _roomGrades.TryGetValue(grade, out roomGrade!);

    public ImmutableDictionary<int, RoomGradeMaster> GetAllRoomGrades()
        => _roomGrades;

    public bool TryGetMission(int id, out MissionMaster mission)
        => _missions.TryGetValue(id, out mission!);

    public ImmutableDictionary<int, MissionMaster> GetAllMissions()
        => _missions;
}

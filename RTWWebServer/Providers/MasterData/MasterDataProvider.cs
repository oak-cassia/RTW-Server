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
    private readonly ImmutableDictionary<int, RankMaster> _ranks;

    // 명성→랭크 파생을 위해 RequiredFame 내림차순으로 미리 정렬해 둔다. 조회 시 fame 이상인 첫 임계값의 랭크를 고른다.
    private readonly ImmutableArray<RankMaster> _ranksByFameDescending;

    public MasterDataProvider(IMasterDataLoader loader, ILogger<MasterDataProvider> logger)
    {
        // 시작 시 1회 로드한다. 잘못된 데이터/누락 파일은 로더가 예외를 던져 기동을 막는다(fail-fast).
        var set = loader.Load();
        _characters = set.Characters;
        _furniture = set.Furniture;
        _roomGrades = set.RoomGrades;
        _missions = set.Missions;
        _ranks = set.Ranks;
        _ranksByFameDescending = _ranks.Values.OrderByDescending(r => r.RequiredFame).ToImmutableArray();

        logger.LogInformation(
            "Master data loaded. Characters count: {CharacterCount}, Furniture count: {FurnitureCount}, RoomGrades count: {RoomGradeCount}, Missions count: {MissionCount}, Ranks count: {RankCount}",
            _characters.Count, _furniture.Count, _roomGrades.Count, _missions.Count, _ranks.Count);
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

    public ImmutableDictionary<int, RankMaster> GetAllRanks()
        => _ranks;

    public int GetRankByFame(long fame)
    {
        // RequiredFame 내림차순이므로, fame이 임계값 이상인 첫 항목이 곧 도달 가능한 가장 높은 랭크다.
        foreach (var rank in _ranksByFameDescending)
        {
            if (fame >= rank.RequiredFame)
            {
                return rank.Rank;
            }
        }

        // baseline(RequiredFame 0)이 있으면 여기에 닿지 않는다. 마스터가 비었거나 fame이 모든 임계값 미만이면 0(랭크 없음).
        return 0;
    }
}

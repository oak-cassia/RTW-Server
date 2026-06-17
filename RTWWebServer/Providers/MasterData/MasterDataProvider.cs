using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using RTWWebServer.Configuration;
using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.Providers.MasterData;

public sealed class MasterDataProvider : IMasterDataProvider, IDisposable
{
    private ImmutableDictionary<int, CharacterMaster> _characters = ImmutableDictionary<int, CharacterMaster>.Empty;
    private ImmutableDictionary<int, FurnitureMaster> _furniture = ImmutableDictionary<int, FurnitureMaster>.Empty;
    private ImmutableDictionary<int, RoomGradeMaster> _roomGrades = ImmutableDictionary<int, RoomGradeMaster>.Empty;
    private readonly IDisposable? _reloader;
    private readonly ILogger<MasterDataProvider> _logger;

    public MasterDataProvider(IOptionsMonitor<MasterDataOptions> monitor, ILogger<MasterDataProvider> logger)
    {
        _logger = logger;

        // 시작 시에는 실패를 그대로 던져 기동을 중단시킨다 (fail fast)
        BuildSnapshot(monitor.CurrentValue);

        // 런타임 리로드 실패는 프로세스를 죽이는 대신 기존 스냅샷을 유지한다
        // (파일 워처 콜백에서 던진 예외는 처리되지 않으면 프로세스를 종료시킬 수 있음)
        _reloader = monitor.OnChange(options =>
        {
            try
            {
                BuildSnapshot(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rebuild master data snapshot on reload. Keeping previous snapshot.");
            }
        });
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

    public void Dispose()
        => _reloader?.Dispose();

    private void BuildSnapshot(MasterDataOptions options)
    {
        var charactersDict = options.Characters.ToImmutableDictionary(c => c.Id);
        var furnitureDict = options.Furniture.ToImmutableDictionary(f => f.Id);
        var roomGradesDict = options.RoomGrades.ToImmutableDictionary(g => g.Grade);

        Interlocked.Exchange(ref _characters, charactersDict);
        Interlocked.Exchange(ref _furniture, furnitureDict);
        Interlocked.Exchange(ref _roomGrades, roomGradesDict);

        _logger.LogInformation(
            "Master data snapshot rebuilt. Characters count: {CharacterCount}, Furniture count: {FurnitureCount}, RoomGrades count: {RoomGradeCount}",
            charactersDict.Count, furnitureDict.Count, roomGradesDict.Count);
    }
}
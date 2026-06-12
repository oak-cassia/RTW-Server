using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using RTWWebServer.Configuration;
using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.Providers.MasterData;

public sealed class MasterDataProvider : IMasterDataProvider, IDisposable
{
    private ImmutableDictionary<int, CharacterMaster> _characters = ImmutableDictionary<int, CharacterMaster>.Empty;
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

    public void Dispose()
        => _reloader?.Dispose();

    private void BuildSnapshot(MasterDataOptions options)
    {
        var charactersDict = options.Characters.ToImmutableDictionary(c => c.Id);

        Interlocked.Exchange(ref _characters, charactersDict);

        _logger.LogInformation("Master data snapshot rebuilt. Characters count: {Count}", charactersDict.Count);
    }
}
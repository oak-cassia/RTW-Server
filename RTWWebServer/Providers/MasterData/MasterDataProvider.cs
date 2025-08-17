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

        BuildSnapshot(monitor.CurrentValue);
        _reloader = monitor.OnChange(BuildSnapshot);
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
using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using RTWWebServer.Configuration;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Services;

public sealed class MasterDataService : IMasterDataService, IDisposable
{
    private ImmutableDictionary<int, CharacterMaster> _characters = ImmutableDictionary<int, CharacterMaster>.Empty;
    private readonly IDisposable? _reloader;
    private readonly ILogger<MasterDataService> _logger;

    public MasterDataService(IOptionsMonitor<MasterDataOptions> monitor, ILogger<MasterDataService> logger)
    {
        _logger = logger;

        BuildSnapshot(monitor.CurrentValue);
        _reloader = monitor.OnChange(BuildSnapshot);
    }

    public bool TryGetCharacter(int id, out CharacterMaster character)
        => _characters.TryGetValue(id, out character!);

    public IReadOnlyCollection<CharacterMaster> GetAllCharacters()
        => _characters.Values.ToList();

    public void Dispose()
        => _reloader?.Dispose();

    private void BuildSnapshot(MasterDataOptions options)
    {
        var charactersDict = options.Characters.ToImmutableDictionary(c => c.Id);

        Interlocked.Exchange(ref _characters, charactersDict);

        _logger.LogInformation("Master data snapshot rebuilt. Characters count: {Count}", charactersDict.Count);
    }
}
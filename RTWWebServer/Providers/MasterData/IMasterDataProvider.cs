using RTWWebServer.MasterData.Models;

namespace RTWWebServer.Providers.MasterData;

public interface IMasterDataProvider
{
    bool TryGetCharacter(int id, out CharacterMaster character);
    IReadOnlyCollection<CharacterMaster> GetAllCharacters();
}
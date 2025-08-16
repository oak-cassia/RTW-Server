using RTWWebServer.Data.Entities;

namespace RTWWebServer.Services;

public interface IMasterDataService
{
    bool TryGetCharacter(int id, out CharacterMaster character);
    IReadOnlyCollection<CharacterMaster> GetAllCharacters();
}
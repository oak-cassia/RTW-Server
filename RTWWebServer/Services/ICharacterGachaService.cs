using RTWWebServer.DTOs.Response;

namespace RTWWebServer.Services;

public interface ICharacterGachaService
{
    Task<CharacterGachaResponse> PerformGachaAsync(long userId, int gachaType, int count);
    Task<List<PlayerCharacterInfo>> GetPlayerCharactersAsync(long userId);
}
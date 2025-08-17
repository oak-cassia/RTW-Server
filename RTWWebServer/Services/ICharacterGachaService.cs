using RTWWebServer.DTOs;
using RTWWebServer.DTOs.Response;

namespace RTWWebServer.Services;

public interface ICharacterGachaService
{
    Task<CharacterGachaResult> PerformGachaAsync(long userId, int gachaType, int count);
    Task<PlayerCharacterInfo[]> GetPlayerCharactersAsync(long userId);
}
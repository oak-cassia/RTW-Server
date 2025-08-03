using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByAccountIdAsync(long accountId);
    Task<User?> GetByNicknameAsync(string nickname);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    void Update(User user);
    void Delete(User user);
    Task<User?> GetByMainCharacterIdAsync(long characterId);
    Task<bool> IsNicknameTakenAsync(string nickname);
}
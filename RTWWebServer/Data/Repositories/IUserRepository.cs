using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByGuidAsync(string guid);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByNicknameAsync(string nickname);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(long id);
    Task<bool> ExistsByGuidAsync(string guid);
    Task<bool> ExistsByEmailAsync(string email);
}

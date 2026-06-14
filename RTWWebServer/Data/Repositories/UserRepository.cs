using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class UserRepository(GameDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(long id)
    {
        return await dbContext.Users.FindAsync(id);
    }

    public async Task<User?> GetByIdAsNoTrackingAsync(long id)
    {
        return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<bool> TryDeductPremiumCurrencyAsync(long userId, long cost, CancellationToken ct = default)
    {
        // 잔액이 충분할 때만 차감하는 조건부 UPDATE. 영향 행 수로 성공 여부를 판정한다.
        // WHERE의 잔액 비교는 DB가 행을 잠근 채 최신 커밋 값으로 평가하므로, 검사와 차감 사이에
        // 끼어들 틈이 없어 잔액이 음수가 되거나 두 요청이 같은 잔액을 중복 차감할 수 없다.
        var affected = await dbContext.Users
            .Where(u => u.Id == userId && u.PremiumCurrency >= cost)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PremiumCurrency, u => u.PremiumCurrency - cost), ct);

        return affected > 0;
    }

    public async Task<User?> GetByAccountIdAsync(long accountId)
    {
        return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
    }

    public async Task<User?> GetByNicknameAsync(string nickname)
    {
        return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Nickname == nickname);
    }

    public async Task<User> CreateAsync(User user)
    {
        await dbContext.Users.AddAsync(user);
        return user;
    }

    public void Update(User user)
    {
        dbContext.Users.Update(user);
    }
}
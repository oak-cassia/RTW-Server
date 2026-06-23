using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByIdAsNoTrackingAsync(long id);
    Task<User?> GetByAccountIdAsync(long accountId);

    /// <summary>
    /// 잔액이 충분할 때만 프리미엄 재화를 차감하는 조건부 UPDATE. 차감에 성공하면 true.
    /// 검사와 차감이 단일 SQL 문에서 일어나므로, 분산락 없이도 잔액이 음수가 될 수 없다.
    /// </summary>
    Task<bool> TryDeductPremiumCurrencyAsync(long userId, long cost, CancellationToken ct = default);

    /// <summary>
    /// 스태미나가 충분할 때만 차감하는 조건부 UPDATE. 차감에 성공하면 true.
    /// TryDeductPremiumCurrencyAsync와 동일하게 검사·차감이 단일 SQL이라 음수가 될 수 없다.
    /// </summary>
    Task<bool> TryConsumeStaminaAsync(long userId, int cost, CancellationToken ct = default);

    /// <summary>
    /// 임무 보상(명성·골드·경험치)을 단일 UPDATE로 가산한다. 증가만 하므로 조건이 필요 없다.
    /// </summary>
    Task ApplyMissionRewardsAsync(long userId, long fame, long gold, long exp, CancellationToken ct = default);

    Task<User?> GetByNicknameAsync(string nickname);
    Task<User> CreateAsync(User user);
    void Update(User user);
}
using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.Data.Entities;

public class User(
    long accountId,
    string nickname,
    int level,
    long currentExp,
    int currentStamina,
    int maxStamina,
    DateTime lastStaminaRecharge,
    long premiumCurrency,
    long freeCurrency,
    int mainCharacterId,
    DateTime createdAt,
    DateTime updatedAt)
{
    public long Id { get; set; }

    public long AccountId { get; set; } = accountId;

    public string Nickname { get; set; } = nickname;

    [Range(1, int.MaxValue, ErrorMessage = "Level must be at least 1")]
    public int Level { get; set; } = level;

    [Range(0, long.MaxValue, ErrorMessage = "CurrentExp cannot be negative")]
    public long CurrentExp { get; set; } = currentExp;

    [Range(0, int.MaxValue, ErrorMessage = "CurrentStamina cannot be negative")]
    public int CurrentStamina { get; set; } = currentStamina;

    [Range(1, int.MaxValue, ErrorMessage = "MaxStamina must be at least 1")]
    public int MaxStamina { get; set; } = maxStamina;

    public DateTime LastStaminaRecharge { get; set; } = lastStaminaRecharge;

    [Range(0, long.MaxValue, ErrorMessage = "PremiumCurrency cannot be negative")]
    // 향후 환불, 보정 등으로 음수 허용이 필요할 수 있음 (현재는 0 이상만 허용)
    public long PremiumCurrency { get; set; } = premiumCurrency;

    [Range(0, long.MaxValue, ErrorMessage = "FreeCurrency cannot be negative")]
    // 향후 환불, 보정 등으로 음수 허용이 필요할 수 있음 (현재는 0 이상만 허용)
    public long FreeCurrency { get; set; } = freeCurrency;

    // 명성. 임무 보상으로 누적되며 0에서 시작한다. 가입 흐름을 건드리지 않도록 생성자 인자가 아닌
    // 기본값 프로퍼티로 두고, 증감은 UserRepository의 단일 UPDATE로만 수행한다.
    [Range(0, long.MaxValue, ErrorMessage = "Fame cannot be negative")]
    public long Fame { get; set; }

    public int MainCharacterId { get; set; } = mainCharacterId;

    public DateTime CreatedAt { get; set; } = createdAt;

    public DateTime UpdatedAt { get; set; } = updatedAt;
}
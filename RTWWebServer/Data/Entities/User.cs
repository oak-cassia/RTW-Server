namespace RTWWebServer.Data.Entities;

public class User(
    long id, 
    string? guid, 
    string? email, 
    int userType, 
    string? nickname, 
    int level,
    long currentExp,
    int currentStamina,
    int maxStamina,
    DateTime? lastStaminaRecharge,
    long premiumCurrency,
    long freeCurrency,
    long? mainCharacterId,
    DateTime createdAt, 
    DateTime updatedAt)
{
    private User()
        : this(0, string.Empty, string.Empty, 0, string.Empty, 0, 0, 0, 0, DateTime.UtcNow, 0, 0, null, default, default)
    {
    }

    public long Id { get; set; } = id;
    public string? Guid { get; set; } = guid;
    public string? Email { get; set; } = email;
    public int UserType { get; set; } = userType;
    public string? Nickname { get; set; } = nickname;
    public int Level { get; set; } = level;
    public long CurrentExp { get; set; } = currentExp;
    public int CurrentStamina { get; set; } = currentStamina;
    public int MaxStamina { get; set; } = maxStamina;
    public DateTime LastStaminaRecharge { get; set; } = lastStaminaRecharge ?? DateTime.UtcNow;
    public long PremiumCurrency { get; set; } = premiumCurrency;
    public long FreeCurrency { get; set; } = freeCurrency;
    public long? MainCharacterId { get; set; } = mainCharacterId;
    public DateTime CreatedAt { get; set; } = createdAt;
    public DateTime UpdatedAt { get; set; } = updatedAt;
}
namespace RTWWebServer.DTOs;

public class UserInfo
{
    public long Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public int Level { get; set; }
    public long CurrentExp { get; set; }
    public int CurrentStamina { get; set; }
    public int MaxStamina { get; set; }
    public long PremiumCurrency { get; set; }
    public long FreeCurrency { get; set; }
    public int MainCharacterId { get; set; }
}

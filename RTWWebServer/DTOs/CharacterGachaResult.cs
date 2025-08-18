namespace RTWWebServer.DTOs;

public class CharacterGachaResult
{
    public List<int> CharacterMasterIds { get; set; } = new();
    public long RemainingPremiumCurrency { get; set; }
    public long RemainingFreeCurrency { get; set; }
}
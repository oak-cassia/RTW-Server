namespace RTWWebServer.DTOs.Response;

public class CharacterGachaResponse
{
    public List<int> CharacterMasterIds { get; set; } = new();
    public long RemainingPremiumCurrency { get; set; }
    public long RemainingFreeCurrency { get; set; }
}
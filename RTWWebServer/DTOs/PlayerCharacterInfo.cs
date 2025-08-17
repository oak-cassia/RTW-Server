namespace RTWWebServer.DTOs;

public class PlayerCharacterInfo
{
    public long Id { get; set; }
    public int CharacterMasterId { get; set; }
    public int Level { get; set; }
    public long CurrentExp { get; set; }
    public DateTime ObtainedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
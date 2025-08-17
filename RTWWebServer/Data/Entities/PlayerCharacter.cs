using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.Data.Entities;

public class PlayerCharacter(
    long userId,
    int characterMasterId,
    int level,
    long currentExp,
    DateTime obtainedAt)
{
    public long Id { get; set; }

    public long UserId { get; set; } = userId;

    public int CharacterMasterId { get; set; } = characterMasterId;

    [Range(1, int.MaxValue, ErrorMessage = "Level must be at least 1")]
    public int Level { get; set; } = level;

    [Range(0, long.MaxValue, ErrorMessage = "CurrentExp cannot be negative")]
    public long CurrentExp { get; set; } = currentExp;

    public DateTime ObtainedAt { get; set; } = obtainedAt;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
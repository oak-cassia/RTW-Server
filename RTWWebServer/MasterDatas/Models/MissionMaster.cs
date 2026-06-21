using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.MasterDatas.Models;

public sealed class MissionMaster
{
    [Range(1, int.MaxValue)]
    public int Id { get; init; }

    [Required, MinLength(1)]
    public string Name { get; init; } = "";

    [Range(0, int.MaxValue)]
    public int StaminaCost { get; init; }

    [Range(1, int.MaxValue)]
    public int StartingMental { get; init; }

    [Range(0, long.MaxValue)]
    public long RewardFame { get; init; }

    [Range(0, long.MaxValue)]
    public long RewardGold { get; init; }

    public MissionStage[] Stages { get; init; } = [];
}

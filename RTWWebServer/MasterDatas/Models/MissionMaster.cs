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

    // 이 임무를 진행하기 위한 최소 플레이어 랭크(명성에서 파생). 0이면 랭크 게이트 없음.
    [Range(0, int.MaxValue)]
    public int RequiredRank { get; init; }

    [Range(1, int.MaxValue)]
    public int StartingMental { get; init; }

    [Range(0, long.MaxValue)]
    public long RewardFame { get; init; }

    [Range(0, long.MaxValue)]
    public long RewardGold { get; init; }

    [Range(0, long.MaxValue)]
    public long RewardExp { get; init; }

    public MissionStage[] Stages { get; init; } = [];
}

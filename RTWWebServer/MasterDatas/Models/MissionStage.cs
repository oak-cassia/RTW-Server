using System.ComponentModel.DataAnnotations;
using RTWWebServer.Game.Mission;

namespace RTWWebServer.MasterDatas.Models;

// 임무의 한 관문. 어떤 스탯을 얼마의 요구치로 시험하는지, 실패 시 멘탈을 얼마나 깎는지 정의한다.
// (JSON에서 Stat은 StatKind enum의 정수값으로 표기: 0=Portfolio, 1=Development, 2=JobSearching)
public sealed class MissionStage
{
    public StatKind Stat { get; init; }

    [Range(0, int.MaxValue)]
    public int RequiredScore { get; init; }

    [Range(0, int.MaxValue)]
    public int MentalPenalty { get; init; }

    public string FlavorText { get; init; } = "";
}

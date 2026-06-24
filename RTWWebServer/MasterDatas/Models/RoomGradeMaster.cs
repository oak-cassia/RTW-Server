using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.MasterDatas.Models;

public sealed class RoomGradeMaster
{
    [Range(1, int.MaxValue)]
    public int Grade { get; init; }

    [Range(1, 1000)]
    public int Width { get; init; }

    [Range(1, 1000)]
    public int Height { get; init; }

    // 이 등급으로 증축하기 위한 최소 플레이어 랭크(명성에서 파생). 0이면 랭크 게이트 없음.
    // 1등급(기본)은 증축 대상이 아니므로 의미가 없다.
    [Range(0, int.MaxValue)]
    public int RequiredRank { get; init; }

    // 이 등급으로 증축하는 비용. ExpandCurrency 재화에서 차감한다. 1등급은 증축 대상이 아니라 무의미.
    [Range(0, long.MaxValue)]
    public long ExpandCost { get; init; }

    // ExpandCost를 차감할 재화 종류.
    public CurrencyType ExpandCurrency { get; init; }
}

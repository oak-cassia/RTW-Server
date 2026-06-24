using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.MasterDatas.Models;

// 누적 명성(Fame)으로 도달하는 플레이어 랭크의 임계값 테이블. 랭크 자체는 저장하지 않고
// User.Fame에서 매번 파생한다(provider.GetRankByFame). 따라서 여기엔 임계값만 둔다.
public sealed class RankMaster
{
    [Range(1, int.MaxValue)]
    public int Rank { get; init; }

    // 이 랭크에 도달하기 위한 누적 명성 임계값. 가장 낮은 랭크는 0(가입 시점 baseline).
    [Range(0, long.MaxValue)]
    public long RequiredFame { get; init; }
}

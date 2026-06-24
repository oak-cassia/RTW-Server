namespace RTWWebServer.DTOs;

// 진행 가능한 임무 목록 응답. 명성에서 파생한 현재 랭크로 필터된 임무만 담는다.
public class MissionListDto
{
    public MissionSummaryDto[] Missions { get; set; } = [];
}

// 목록용 임무 요약. 전투 스테이지 세부는 빼고 선택/표시에 필요한 필드만 노출한다.
public class MissionSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int StaminaCost { get; set; }
    public int RequiredRank { get; set; }
    public long RewardFame { get; set; }
    public long RewardGold { get; set; }
    public long RewardExp { get; set; }
}

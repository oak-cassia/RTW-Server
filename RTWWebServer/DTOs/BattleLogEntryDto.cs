using RTWWebServer.Game.Mission;

namespace RTWWebServer.DTOs;

// 전투 로그 한 줄. 결정론적 시뮬레이션 결과를 클라이언트가 그대로 재생할 수 있도록 구조화한다.
public class BattleLogEntryDto
{
    public int Index { get; set; }
    public string Stage { get; set; } = "";
    public StatKind Stat { get; set; }
    public int Roll { get; set; }
    public int Required { get; set; }
    public bool Passed { get; set; }
    public int MentalAfter { get; set; }
    public string Message { get; set; } = "";
}

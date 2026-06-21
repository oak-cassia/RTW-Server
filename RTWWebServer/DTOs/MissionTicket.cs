namespace RTWWebServer.DTOs;

// Redis에 저장되는 임무 예약 정보. start가 기록하고 end가 읽어 검증한다.
// (게임서버 인증의 session_{userId}와 동일한 서버 간 핸드오프 패턴.)
// 보상 금액은 여기 담지 않는다 — end에서 MissionId로 마스터에서 재계산해 위조를 막는다.
public class MissionTicket
{
    public string TicketId { get; set; } = "";
    public long UserId { get; set; }
    public int MissionId { get; set; }

    // 결정론 시뮬레이션 시드. 같은 시드 → 같은 결과(재현/검증용).
    public long Seed { get; set; }
}

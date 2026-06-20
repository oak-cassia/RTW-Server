namespace RTWWebServer.DTOs;

// start 응답. 클라이언트는 이 티켓으로 게임서버에 접속해 전투를 수행하고, 끝나면 end에 제출한다.
public class MissionTicketDto
{
    public string TicketId { get; set; } = "";

    // 시드를 echo해 클라이언트가 전투를 동일하게 재생/검증할 수 있게 한다.
    public long Seed { get; set; }

    // 향후 확장 지점: 게임서버 접속 정보(host/port)나 파티 식별자가 들어갈 자리.
}

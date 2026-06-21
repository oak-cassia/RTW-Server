using RTWWebServer.DTOs;

namespace RTWWebServer.Services;

public interface IMissionService
{
    // 예약: 스태미나를 차감하고 시드/티켓을 발급한다. 전투는 (게임서버에서) 이후에 수행된다.
    // characterId는 투입할 보유 캐릭터의 마스터 ID이며, 소유 여부를 서버가 검증한다.
    Task<MissionTicketDto> StartMissionAsync(long userId, int missionId, int characterId);

    // 정산: 전투 결과로 보상을 지급한다. 같은 티켓으로 두 번 호출해도 한 번만 지급된다(멱등).
    Task<MissionResultDto> CompleteMissionAsync(long userId, string ticketId);
}

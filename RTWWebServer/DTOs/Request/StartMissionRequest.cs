using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.DTOs.Request;

public record StartMissionRequest(
    [Range(1, int.MaxValue, ErrorMessage = "MissionId must be greater than 0")]
    int MissionId,
    // 임무에 투입할 캐릭터(보유 캐릭터의 마스터 ID). 서버가 소유 여부를 검증한다.
    [Range(1, int.MaxValue, ErrorMessage = "CharacterId must be greater than 0")]
    int CharacterId);

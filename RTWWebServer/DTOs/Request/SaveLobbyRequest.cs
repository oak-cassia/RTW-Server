using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.DTOs.Request;

public record SaveLobbyRequest(
    [Required]
    LobbyFurniturePlacement[] Items);

public record LobbyFurniturePlacement(
    [Range(1, int.MaxValue, ErrorMessage = "FurnitureMasterId must be greater than 0")]
    int FurnitureMasterId,
    int PosX,
    int PosY,
    [AllowedValues(LobbyRotation.Degrees0, LobbyRotation.Degrees90, LobbyRotation.Degrees180, LobbyRotation.Degrees270,
        ErrorMessage = "Rotation must be one of 0, 90, 180, 270")]
    int Rotation);

// 로비 가구 회전 규칙의 단일 소스. 회전은 90도 단위만 허용한다. 그리드 점유(footprint)는 90도
// 배수에서만 정의되며, 90/270도에서 가로·세로가 뒤바뀐다. 애트리뷰트 인자로도 써야 하므로 const다.
public static class LobbyRotation
{
    public const int Degrees0 = 0;
    public const int Degrees90 = 90;
    public const int Degrees180 = 180;
    public const int Degrees270 = 270;
}

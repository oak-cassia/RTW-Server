using System.Text.Json.Serialization;

namespace RTWServer.Authentication;

/// <summary>
/// 웹 서버 RTWWebServer.DTOs.UserSession이 Redis에 직렬화된 형태를 읽기 위한 복제 DTO다.
/// 웹 서버는 System.Text.Json 기본 옵션으로 직렬화하므로 필드명(UserId, Token, Nickname)은
/// 웹 서버와 반드시 동기화되어야 한다. 한쪽이 바뀌면 인증이 조용히 깨진다.
/// </summary>
public class GameUserSession
{
    [JsonPropertyName("UserId")]
    public long UserId { get; set; }

    [JsonPropertyName("Token")]
    public string Token { get; set; } = string.Empty;

    // 닉네임 연동 이전에 발급된 세션엔 이 필드가 없어 null로 역직렬화된다(폴백 처리 필요).
    [JsonPropertyName("Nickname")]
    public string? Nickname { get; set; }
}

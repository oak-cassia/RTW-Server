using RTWWebServer.Enums;

namespace RTWWebServer.DTOs;

public class JwtTokenInfo
{
    public long AccountId { get; set; } // Account 테이블의 ID
    public UserRole? UserRole { get; set; }
    public string? Email { get; set; } // Normal 계정의 경우
    public Guid? Guid { get; set; } // Guest 계정의 경우
    public bool IsValid { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
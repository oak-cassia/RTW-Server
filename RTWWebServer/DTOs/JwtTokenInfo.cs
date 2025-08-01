using RTWWebServer.Enums;

namespace RTWWebServer.DTOs;

public class JwtTokenInfo
{
    public long? UserId { get; set; }
    public UserRole? UserRole { get; set; }
    public string? Email { get; set; }
    public Guid? Guid { get; set; }
    public string? Jti { get; set; }
    public DateTime? Expiration { get; set; }
    
    public bool IsValid => UserId.HasValue && UserRole.HasValue;
}

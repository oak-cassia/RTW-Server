namespace RTWServer.ServerCore.Interface;

/// <summary>
/// 세션 검증 결과. 유효하지 않으면 <see cref="IsValid"/>가 false이고 나머지 필드는 의미가 없다.
/// ServerCore가 인증 DTO에 의존하지 않도록, 검증기가 알아낸 정체성(표시명 등)만 추려서 전달한다.
/// </summary>
public sealed record SessionValidationResult(bool IsValid, string? Nickname)
{
    public static readonly SessionValidationResult Invalid = new(false, null);
}

public interface ISessionValidator
{
    /// <summary>
    /// (userId, token) 쌍이 웹 서버가 발급한 유효한 세션과 일치하는지 검증하고,
    /// 일치하면 세션에 담긴 정체성 정보(표시명 등)를 함께 돌려준다.
    /// </summary>
    Task<SessionValidationResult> ValidateAsync(long userId, string token, CancellationToken cancellationToken = default);
}

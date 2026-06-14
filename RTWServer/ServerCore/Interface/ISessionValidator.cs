namespace RTWServer.ServerCore.Interface;

public interface ISessionValidator
{
    /// <summary>
    /// (userId, token) 쌍이 웹 서버가 발급한 유효한 세션과 일치하는지 검증한다.
    /// </summary>
    Task<bool> ValidateAsync(long userId, string token, CancellationToken cancellationToken = default);
}

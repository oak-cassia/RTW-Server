namespace RTWWebServer.Cache;

public class RemoteCacheKeyGenerator : IRemoteCacheKeyGenerator
{
    public string GenerateAccountLockKey(long accountId)
    {
        return $"lock:account:{accountId}";
    }

    public string GenerateUserLockKey(long userId)
    {
        return $"lock:user:{userId}";
    }

    public string GenerateUserSessionKey(long userId)
    {
        return $"session_{userId}";
    }

    public string GeneratePlayerCharactersKey(long userId)
    {
        return $"player:characters:{userId}";
    }

    // 임무 한 판의 예약 정보(start가 기록, end가 조회). 게임서버 인증의 session_{userId}와 같은 역할.
    public string GenerateMissionTicketKey(string ticketId)
    {
        return $"mission:ticket:{ticketId}";
    }

    // 전투 결과(게임서버가 기록, end가 조회). 스켈레톤에선 start의 스텁이 대신 기록한다.
    public string GenerateMissionResultKey(string ticketId)
    {
        return $"mission:result:{ticketId}";
    }

    // 정산(end) 직렬화용 락. 동시 정산으로 보상이 중복 지급되지 않도록 한다.
    public string GenerateMissionSettleLockKey(string ticketId)
    {
        return $"lock:mission:{ticketId}";
    }
}
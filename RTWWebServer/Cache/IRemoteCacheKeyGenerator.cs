namespace RTWWebServer.Cache;

public interface IRemoteCacheKeyGenerator
{
    string GenerateAccountLockKey(long accountId);
    string GenerateUserLockKey(long userId);
    string GenerateUserSessionKey(long userId);
    string GeneratePlayerCharactersKey(long userId);
    string GenerateMissionTicketKey(string ticketId);
    string GenerateMissionResultKey(string ticketId);
    string GenerateMissionSettleLockKey(string ticketId);
}
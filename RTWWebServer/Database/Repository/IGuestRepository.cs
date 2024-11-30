using NetworkDefinition.ErrorCode;
using RTWWebServer.Database.Entity;

namespace RTWWebServer.Database.Repository;

public interface IGuestRepository
{
    public Task<Guest?> FindByGuidAsync(byte[] guestGuid);
    public Task<long> CreateGuestAsync(byte[] guestGuid);
}
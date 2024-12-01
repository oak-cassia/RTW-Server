using RTWWebServer.Entity;

namespace RTWWebServer.Repository;

public interface IGuestRepository
{
    public Task<Guest?> FindByGuidAsync(byte[] guestGuid);
    public Task<long> CreateGuestAsync(byte[] guestGuid);
}
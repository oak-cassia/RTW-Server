using RTWWebServer.Entities;

namespace RTWWebServer.Repositories;

public interface IGuestRepository
{
    public Task<Guest?> FindByGuidAsync(byte[] guestGuid);
    public Task<long> CreateGuestAsync(byte[] guestGuid);
}
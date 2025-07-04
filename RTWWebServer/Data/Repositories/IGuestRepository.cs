using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public interface IGuestRepository
{
    public Task<Guest?> FindByGuidAsync(byte[] guestGuid);
    public Task<long> CreateGuestAsync(byte[] guestGuid);
}
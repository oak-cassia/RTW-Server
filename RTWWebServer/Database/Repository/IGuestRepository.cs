using NetworkDefinition.ErrorCode;
using RTWWebServer.Database.Data;

namespace RTWWebServer.Database.Repository;

public interface IGuestRepository
{
    public Task<Guest?> FindByGuidAsync(byte[] guestGuid);
    public Task<WebServerErrorCode> CreateGuestAsync(byte[] guestGuid);
}
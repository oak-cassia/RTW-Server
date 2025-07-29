using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data.Repositories;

public class GuestRepository(AccountDbContext dbContext) : IGuestRepository
{
    public async Task<Guest?> FindByGuidAsync(byte[] guestGuid)
    {
        return await dbContext.Guests
            .FirstOrDefaultAsync(g => g.Guid == new Guid(guestGuid));
    }

    public Task CreateGuestAsync(Guest guest)
    {
        dbContext.Guests.Add(guest);
        return Task.CompletedTask;
    }
}
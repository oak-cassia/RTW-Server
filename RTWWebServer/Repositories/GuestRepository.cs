using Microsoft.EntityFrameworkCore;
using RTWWebServer.Database;
using RTWWebServer.Entities;

namespace RTWWebServer.Repositories;

public class GuestRepository(AccountDbContext dbContext) : IGuestRepository
{
    public async Task<Guest?> FindByGuidAsync(byte[] guestGuid)
    {
        return await dbContext.Guests
            .FirstOrDefaultAsync(g => g.Guid == new Guid(guestGuid));
    }

    public async Task<long> CreateGuestAsync(byte[] guestGuid)
    {
        Guest guest = new Guest(new Guid(guestGuid));

        dbContext.Guests.Add(guest);
        await dbContext.SaveChangesAsync();
        return guest.Id;
    }
}
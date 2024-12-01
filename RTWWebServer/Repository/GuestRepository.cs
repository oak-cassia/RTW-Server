using MySqlConnector;
using RTWWebServer.Database;
using RTWWebServer.Entity;

namespace RTWWebServer.Repository;

public class GuestRepository(IDatabaseContextProvider databaseContextProvider) : IGuestRepository
{
    private readonly IDatabaseContext _databaseContext = databaseContextProvider.GetDatabaseContext("Account");

    public async Task<Guest?> FindByGuidAsync(byte[] guestGuid)
    {
        string query = $"""
                        SELECT *
                        FROM Guest 
                        WHERE guid = @{nameof(guestGuid)} 
                        """;

        await using IDatabaseCommand command = await _databaseContext.CreateCommandAsync(query);

        command.AddParameter($"@{nameof(guestGuid)}", guestGuid);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Guest(reader.GetInt64("id"), reader.GetGuid("guid"));
        }

        return null;
    }

    public async Task<long> CreateGuestAsync(byte[] guestGuid)
    {
        string query = $"""
                        INSERT INTO Guest (guid)
                        VALUES (@{nameof(guestGuid)})
                        """;

        await using IDatabaseCommand command = await _databaseContext.CreateCommandAsync(query);

        command.AddParameter($"@{nameof(guestGuid)}", guestGuid);

        return await command.ExecuteNonQueryAsync();
    }
}
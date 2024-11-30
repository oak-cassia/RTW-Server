using MySqlConnector;
using RTWWebServer.Database.Entity;

namespace RTWWebServer.Database.Repository;

public class GuestRepository(AccountDatabaseContext databaseContext) : IGuestRepository
{
    public async Task<Guest?> FindByGuidAsync(byte[] guestGuid)
    {
        string query = $"""
                        SELECT *
                        FROM Guest 
                        WHERE guid = @{nameof(guestGuid)} 
                        """;

        await using MySqlCommand command = await databaseContext.CreateCommandAsync(query);

        command.Parameters.Add($"@{nameof(guestGuid)}", MySqlDbType.VarBinary).Value = guestGuid;

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

        await using MySqlCommand command = await databaseContext.CreateCommandAsync(query);

        command.Parameters.AddWithValue($"@{nameof(guestGuid)}", guestGuid);

        await command.ExecuteNonQueryAsync();

        return command.LastInsertedId;
    }
}
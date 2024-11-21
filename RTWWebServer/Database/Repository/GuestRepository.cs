using MySqlConnector;
using RTWWebServer.Database.Data;

namespace RTWWebServer.Database.Repository;

public class GuestRepository(IMySqlConnectionProvider connectionProvider) : IGuestRepository
{
    public async Task<Guest?> FindByGuidAsync(byte[] guestGuid)
    {
        string query = $"""
                        SELECT *
                        FROM Guest 
                        WHERE guid = @{nameof(guestGuid)} 
                        """;

        var connection = await connectionProvider.GetAccountConnectionAsync();

        await using var command = new MySqlCommand(query, connection);

        command.Parameters.Add($"@{nameof(guestGuid)}", MySqlDbType.VarBinary).Value = guestGuid;

        await using var reader = await command.ExecuteReaderAsync();
        
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

        var connection = await connectionProvider.GetAccountConnectionAsync();

        await using var command = new MySqlCommand(query, connection);

        command.Parameters.AddWithValue($"@{nameof(guestGuid)}", guestGuid);

        await command.ExecuteNonQueryAsync();

        return command.LastInsertedId;
    }
}
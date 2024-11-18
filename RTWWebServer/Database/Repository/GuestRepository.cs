using MySqlConnector;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Database.Data;

namespace RTWWebServer.Database.Repository;

public class GuestRepository(MySqlConnection connection) : IGuestRepository
{
    public async Task<Guest?> FindByGuidAsync(byte[] guestGuid)
    {
        string query = $"""
                        SELECT *
                        FROM Guest 
                        WHERE guid = @{nameof(guestGuid)} 
                        """;

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.Add($"@{nameof(guestGuid)}", MySqlDbType.VarBinary).Value = guestGuid;

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Guest(reader.GetInt32("id"), reader.GetGuid("guid"));
        }

        return null;
    }

    public Task<WebServerErrorCode> CreateGuestAsync(byte[] guestGuid)
    {
        string query = $"""
                        INSERT INTO Guest (guid)
                        VALUES (@{nameof(guestGuid)})
                        """;
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue($"@{nameof(guestGuid)}", guestGuid);
        command.ExecuteNonQuery();

        return Task.FromResult(WebServerErrorCode.Success);
    }
}
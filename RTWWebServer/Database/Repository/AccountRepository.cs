using MySqlConnector;
using RTWWebServer.Database.Data;

namespace RTWWebServer.Database.Repository;

public class AccountRepository(AccountDatabaseContext databaseContext) : IAccountRepository
{
    public async Task<Account?> FindByIdAsync(int id)
    {
        string query = $"""
                        SELECT *
                        FROM Account 
                        WHERE id = @{nameof(id)}
                        """;

        await using var command = await databaseContext.CreateCommandAsync(query);

        command.Parameters.AddWithValue($"@{nameof(id)}", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Account
            (
                reader.GetInt64("Id"),
                reader.GetString("UserName"),
                reader.GetString("Email"),
                reader.GetString("Password"),
                reader.GetString("Salt")
            );
        }

        return null;
    }

    public async Task<Account?> FindByEmailAsync(string email)
    {
        string query = $"""
                        SELECT *
                        FROM Account 
                        WHERE email = @{nameof(email)} 
                        """;

        await using var command = await databaseContext.CreateCommandAsync(query);

        command.Parameters.AddWithValue($"@{nameof(email)}", email);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Account
            (
                reader.GetInt64("Id"),
                reader.GetString("UserName"),
                reader.GetString("Email"),
                reader.GetString("Password"),
                reader.GetString("Salt")
            );
        }

        return null;
    }

    public async Task<bool> CreateAccountAsync(string username, string email, string password, string salt)
    {
        string query = $"""
                        INSERT INTO Account (UserName, Email, Password, Salt)
                        VALUES (@{nameof(username)}, @{nameof(email)}, @{nameof(password)}, @{nameof(salt)})
                        """;

        await using var command = await databaseContext.CreateCommandAsync(query);

        command.Parameters.AddWithValue($"@{nameof(username)}", username);
        command.Parameters.AddWithValue($"@{nameof(email)}", email);
        command.Parameters.AddWithValue($"@{nameof(password)}", password);
        command.Parameters.AddWithValue($"@{nameof(salt)}", salt);

        await command.ExecuteNonQueryAsync();

        return command.LastInsertedId > 0;
    }
}
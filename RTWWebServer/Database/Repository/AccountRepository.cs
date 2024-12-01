using MySqlConnector;
using RTWWebServer.Database.Entity;

namespace RTWWebServer.Database.Repository;

public class AccountRepository(IDatabaseContextProvider databaseContextProvider) : IAccountRepository
{
    private readonly IDatabaseContext _databaseContext = databaseContextProvider.GetDatabaseContext("Account");

    public async Task<Account?> FindByIdAsync(int id)
    {
        string query = $"""
                        SELECT *
                        FROM Account 
                        WHERE id = @{nameof(id)}
                        """;

        await using IDatabaseCommand command = await _databaseContext.CreateCommandAsync(query);

        command.AddParameter($"@{nameof(id)}", id);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();

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

        await using IDatabaseCommand command = await _databaseContext.CreateCommandAsync(query);

        command.AddParameter($"@{nameof(email)}", email);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();

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

        await using IDatabaseCommand command = await _databaseContext.CreateCommandAsync(query);

        command.AddParameter($"@{nameof(username)}", username);
        command.AddParameter($"@{nameof(email)}", email);
        command.AddParameter($"@{nameof(password)}", password);
        command.AddParameter($"@{nameof(salt)}", salt);

        int lastInsertedId = await command.ExecuteNonQueryAsync();
        return lastInsertedId > 0;
    }
}
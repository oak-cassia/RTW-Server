using MySqlConnector;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Database.Data;

namespace RTWWebServer.Database.Repository;

public class AccountRepository(MySqlConnection connection) : IAccountRepository
{
    private readonly MySqlConnection _connection = connection;

    public async Task<Account?> FindById(int id)
    {
        try
        {
            string query = $"SELECT * FROM Account WHERE id = @{nameof(id)}";
            using var command = new MySqlCommand(query, _connection);
            command.Parameters.AddWithValue("@nameof(id)", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Account
                (
                    reader.GetInt32("Id"),
                    reader.GetString("UserName"),
                    reader.GetString("Email"),
                    reader.GetString("Password"),
                    reader.GetString("Salt")
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in FindById: {ex.Message}");
        }

        return null;
    }

    public async Task<Account?> FindByEmail(string email)
    {
        try
        {
            string query = $"SELECT * FROM Account WHERE email = @{nameof(email)}";
            await using var command = new MySqlCommand(query, _connection);
            command.Parameters.AddWithValue($"@{nameof(email)}", email);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Account
                (
                    reader.GetInt32("Id"),
                    reader.GetString("UserName"),
                    reader.GetString("Email"),
                    reader.GetString("Password"),
                    reader.GetString("Salt")
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in FindByEmail: {ex.Message}");
        }

        return null;
    }

    public Task<WebServerErrorCode> CreateAccount(string username, string email, string password, string salt)
    {
        try
        {
            string query = "INSERT INTO Account (username, email, password, salt) VALUES (@username, @email, @password, @salt)";
            using var command = new MySqlCommand(query, _connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@password", password);
            command.Parameters.AddWithValue("@salt", salt);

            command.ExecuteNonQuery();
            return Task.FromResult(WebServerErrorCode.Success);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CreateAccount: {ex.Message}");
            return Task.FromResult(WebServerErrorCode.InternalServerError);
        }
    }
}
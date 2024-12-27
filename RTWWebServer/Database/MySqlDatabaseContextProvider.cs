using System.Collections.Concurrent;
using System.Xml;
using Microsoft.Extensions.Options;
using RTWWebServer.Cache;
using RTWWebServer.Configuration;

namespace RTWWebServer.Database;

public class MySqlDatabaseContextProvider(IOptions<DatabaseConfiguration> configuration) : IDatabaseContextProvider
{
    private readonly ConcurrentDictionary<string, IDatabaseContext> _contextCache = new();

    private readonly DatabaseConfiguration _configuration = configuration.Value;

    public IDatabaseContext GetDatabaseContext(string databaseName)
    {
        if (_contextCache.TryGetValue(databaseName, out IDatabaseContext? cachedContext))
        {
            return cachedContext;
        }

        IDatabaseContext newContext = CreateDatabaseContext(databaseName);

        _contextCache[databaseName] = newContext;

        return newContext;
    }

    private IDatabaseContext CreateDatabaseContext(string databaseName)
    {
        return databaseName switch
        {
            "Account" => new MySqlDatabaseContext(_configuration.AccountDatabase),
            "Game" => new MySqlDatabaseContext(_configuration.GameDatabase),
            _ => throw new ArgumentException($"Database name '{databaseName}' is not supported", nameof(databaseName))
        };
    }

    public void Dispose()
    {
        foreach (IDatabaseContext context in _contextCache.Values)
        {
            context.Dispose();
        }

        _contextCache.Clear();
    }
}
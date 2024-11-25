using Microsoft.Extensions.Options;
using RTWWebServer.Configuration;

namespace RTWWebServer.Database;

public class GameDatabaseContext(IOptions<DatabaseConfiguration> configuration) : BaseDatabaseContext(configuration.Value.GameDatabase);
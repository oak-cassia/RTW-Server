using Microsoft.Extensions.Options;
using RTWWebServer.Configuration;

namespace RTWWebServer.Database;

public class AccountDatabaseContext(IOptions<DatabaseConfiguration> configuration)
    : BaseDatabaseContext(configuration.Value.AccountDatabase);
namespace RTWWebServer.Database;

public interface IDatabaseContextProvider : IDisposable
{
    IDatabaseContext GetDatabaseContext(string databaseName); 
}
namespace RTWWebServer.Provider.Authentication;

public interface IPasswordHasher
{
    string GenerateSaltValue();
    string CalcHashedPassword(string password, string salt);
}
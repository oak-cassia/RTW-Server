namespace RTWWebServer.Authentication;

public interface IPasswordHasher
{
    string GetNewSalt();
    string CalcHashedPassword(string password, string salt);
}
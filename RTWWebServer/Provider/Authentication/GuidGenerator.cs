namespace RTWWebServer.Provider.Authentication;

public class GuidGenerator : IGuidGenerator
{
    public Guid GenerateGuid()
    {
        return Guid.NewGuid();
    }
}
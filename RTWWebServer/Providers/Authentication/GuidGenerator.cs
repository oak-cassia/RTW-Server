namespace RTWWebServer.Providers.Authentication;

public class GuidGenerator : IGuidGenerator
{
    public Guid GenerateGuid()
    {
        return Guid.NewGuid();
    }
}
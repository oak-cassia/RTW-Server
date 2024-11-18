namespace RTWWebServer.RequestResponse;

public class GuestLoginRequest(string guestGuid)
{
    public string GuestGuid { get; set; } = guestGuid;
}
namespace RTWWebServer.DTOs.Request;

public class GuestLoginRequest(string guestGuid)
{
    public string GuestGuid { get; set; } = guestGuid;
}
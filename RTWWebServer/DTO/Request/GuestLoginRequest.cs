namespace RTWWebServer.DTO.Request;

public class GuestLoginRequest(string guestGuid)
{
    public string GuestGuid { get; set; } = guestGuid;
}
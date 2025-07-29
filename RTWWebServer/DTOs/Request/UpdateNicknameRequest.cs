namespace RTWWebServer.DTOs.Request;

public record UpdateNicknameRequest(long UserId, string Nickname);
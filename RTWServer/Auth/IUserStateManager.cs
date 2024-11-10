namespace RTWServer.Auth;

public interface IUserStateManager
{
    Task<UserState> GetUserStateAsync(string userId);
}
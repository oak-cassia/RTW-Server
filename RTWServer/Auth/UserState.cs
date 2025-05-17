namespace RTWServer.Auth;

/// <summary>
/// Represents the state of a user in the RTW server
/// </summary>
public class UserState
{
    /// <summary>
    /// The user's ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// The player ID assigned to this user (may differ from UserId)
    /// </summary>
    public int PlayerId { get; set; }
    
    /// <summary>
    /// The user's current authentication token
    /// </summary>
    public string? CurrentAuthToken { get; set; }
    
    /// <summary>
    /// When the user last connected to the server
    /// </summary>
    public DateTime LastConnectionTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether the user is currently connected to the server
    /// </summary>
    public bool IsConnected { get; set; }
}
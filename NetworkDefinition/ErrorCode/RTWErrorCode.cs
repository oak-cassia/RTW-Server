namespace NetworkDefinition.ErrorCode;

public enum RTWErrorCode
{
    // Success (0)
    Success = 0,
    
    // Authentication & Connection Errors (1-99)
    InvalidAuthToken = 1,
    AuthenticationFailed = 2,
    AccountNotFound = 3,
    AuthServerError = 4,
    AlreadyLoggedIn = 5,
    SessionExpired = 6,
    ConnectionClosed = 7,
    
    // Game Server Errors (100-199)
    InternalServerError = 100,
    InvalidRequest = 101,
    InvalidOperation = 102,
    
    // World & Player Errors (200-299)
    WorldNotFound = 200,
    PlayerNotFound = 201,
    PlayerAlreadyExists = 202,
    WorldFull = 203,
    
    // General errors (900-999)
    UnknownError = 999
}
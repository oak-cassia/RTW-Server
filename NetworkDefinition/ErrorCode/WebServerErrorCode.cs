namespace NetworkDefinition.ErrorCode;

public enum WebServerErrorCode
{
    // Web Server Error Codes (1000~1999)
    Success = 1000,
    InvalidEmail = 1001,
    InvalidPassword = 1002,
    AccountNotFound = 1003,
    GuestNotFound = 1004,
    DatabaseError = 1100,
    RemoteCacheError = 1200,
    RemoteCacheLockFailed = 1201, 
 
    InternalServerError = 1999,
}
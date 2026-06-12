namespace NetworkDefinition.ErrorCode;

public enum WebServerErrorCode
{
    // Web Server Error Codes (1000~1999)
    Success = 1000,
    InvalidEmail = 1001,
    InvalidPassword = 1002,
    AccountNotFound = 1003,
    GuestNotFound = 1004,
    InvalidRequestHttpBody = 1005,
    InvalidAuthToken = 1006,
    InsufficientCurrency = 1007,
    InvalidArgument = 1008,
    UserNotFound = 1009,
    DuplicateNickname = 1010,
    DuplicateEmail = 1011,
    InvalidCredentials = 1012, // 이메일/비밀번호 불일치 (어느 쪽이 틀렸는지 노출하지 않음)
    DatabaseError = 1100,
    RemoteCacheError = 1200,
    RemoteCacheLockFailed = 1201, 
 
    InternalServerError = 1999,
}
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
    DuplicateCharacter = 1013, // 동시 가챠가 같은 캐릭터를 뽑아 uk_user_character를 위반 (재시도 가능)
    MissionNotFound = 1014, // 요청한 임무 ID가 마스터에 없음
    InsufficientStamina = 1015, // 임무 입장에 필요한 스태미나가 부족
    MissionResultNotReady = 1016, // 게임서버가 아직 결과를 기록하지 않음 (잠시 후 재시도 가능)
    MissionTicketNotFound = 1017, // 티켓이 없음 (만료/이미 정산됨/유효하지 않음). 소유자 불일치도 동일하게 취급
    DatabaseError = 1100,
    RemoteCacheError = 1200,
    RemoteCacheLockFailed = 1201, 
 
    InternalServerError = 1999,
}
namespace RTWWebServer.GameLog;

/// <summary>
/// economy_event.asset_type 값. ClickHouse LowCardinality(String) 컬럼에 그대로 기록되므로
/// 문자열이 바뀌면 기존 파티션과 값이 분리된다. 반드시 이 상수를 통해서만 참조한다.
/// </summary>
public static class GameLogAssetType
{
    public const string Stamina = "Stamina";
    public const string FreeCurrency = "FreeCurrency";
    public const string PremiumCurrency = "PremiumCurrency";
}

/// <summary>
/// progression_event.progress_type 값. ClickHouse LowCardinality(String) 컬럼에 그대로 기록되므로
/// 문자열이 바뀌면 기존 파티션과 값이 분리된다. 반드시 이 상수를 통해서만 참조한다.
/// </summary>
public static class GameLogProgressType
{
    public const string Fame = "Fame";
    public const string Exp = "Exp";
    public const string Rank = "Rank";
    public const string Character = "Character";
    public const string RoomGrade = "RoomGrade";
}

/// <summary>
/// economy_event/progression_event 공용 action_code 값. 재화·육성 변동을 유발한 게임 행위를 식별한다.
/// </summary>
public static class GameLogActionCode
{
    public const string NewUser = "NewUser";
    public const string Gacha = "Gacha";
    public const string MissionStart = "MissionStart";
    public const string MissionComplete = "MissionComplete";
    public const string ExpandRoom = "ExpandRoom";
}
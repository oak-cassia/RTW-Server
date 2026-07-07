using RTWWebServer.Game.Mission;

namespace RTWWebServer.GameLog;

/// <summary>
/// 게임 로직 이벤트를 ClickHouse 등의 저장소에 로깅하기 위한 인터페이스입니다.
/// 계약: 호출은 논블로킹(Non-blocking)이어야 하며, 내부에서 절대 예외를 throw해서는 안 됩니다.
/// </summary>
public interface IGameEventLogger
{
    void LogEconomyEvent(
        long userId,
        string assetType,
        int assetId,
        long changeAmount,
        long beforeAmount,
        long afterAmount,
        string actionCode,
        string refId);

    void LogMissionBattleEvent(
        long userId,
        string ticketId,
        int missionId,
        int characterId,
        long seed,
        MissionOutcome outcome,
        IReadOnlyList<BattleLogEntry> logs);

    void LogProgressionEvent(
        long userId,
        string progressType,
        int subjectId,
        long changeAmount,
        long beforeValue,
        long afterValue,
        string actionCode,
        string refId);
}

/// <summary>
/// 로깅 비활성화 시 사용되는 No-Op 구현체입니다.
/// </summary>
public sealed class NoOpGameEventLogger : IGameEventLogger
{
    public void LogEconomyEvent(
        long userId,
        string assetType,
        int assetId,
        long changeAmount,
        long beforeAmount,
        long afterAmount,
        string actionCode,
        string refId)
    {
    }

    public void LogMissionBattleEvent(
        long userId,
        string ticketId,
        int missionId,
        int characterId,
        long seed,
        MissionOutcome outcome,
        IReadOnlyList<BattleLogEntry> logs)
    {
    }

    public void LogProgressionEvent(
        long userId,
        string progressType,
        int subjectId,
        long changeAmount,
        long beforeValue,
        long afterValue,
        string actionCode,
        string refId)
    {
    }
}
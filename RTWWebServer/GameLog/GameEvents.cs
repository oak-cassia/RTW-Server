using System;

namespace RTWWebServer.GameLog;

public record EconomyEvent(
    DateTime EventTime,
    long UserId,
    string AssetType,
    int AssetId,
    long ChangeAmount,
    long BeforeAmount,
    long AfterAmount,
    string ActionCode,
    string RefId
);

public record MissionBattleEvent(
    DateTime EventTime,
    long UserId,
    string TicketId,
    int MissionId,
    int CharacterId,
    long Seed,
    string Outcome,
    ushort StageCount,
    int[] StageIndex,
    string[] StageName,
    string[] StageStat,
    int[] StageRoll,
    int[] StageRequired,
    byte[] StagePassed,
    int[] StageMentalAfter
);

public record ProgressionEvent(
    DateTime EventTime,
    long UserId,
    string ProgressType,
    int SubjectId,
    long ChangeAmount,
    long BeforeValue,
    long AfterValue,
    string ActionCode,
    string RefId
);
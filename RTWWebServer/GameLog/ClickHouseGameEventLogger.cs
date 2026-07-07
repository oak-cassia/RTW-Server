using System.Threading.Channels;
using ClickHouse.Driver;
using Microsoft.Extensions.Options;
using RTWWebServer.Configuration;
using RTWWebServer.Game.Mission;

namespace RTWWebServer.GameLog;

public sealed class ClickHouseGameEventLogger : BackgroundService, IGameEventLogger
{
    private readonly ILogger<ClickHouseGameEventLogger> _logger;
    private readonly GameEventLogConfiguration _config;
    private readonly ClickHouseClient _client;
    private readonly Channel<EconomyEvent> _economyChannel;
    private readonly Channel<MissionBattleEvent> _missionBattleChannel;
    private readonly Channel<ProgressionEvent> _progressionChannel;
    private readonly SemaphoreSlim _schemaLock = new(1, 1);
    private bool _schemaReady;

    private static readonly string[] EconomyColumns =
    {
        "event_time", "user_id", "asset_type", "asset_id", "change_amount", "before_amount", "after_amount", "action_code", "ref_id"
    };

    private static readonly string[] MissionBattleColumns =
    {
        "event_time", "user_id", "ticket_id", "mission_id", "character_id", "seed", "outcome", "stage_count",
        "stage_index", "stage_name", "stage_stat", "stage_roll", "stage_required", "stage_passed", "stage_mental_after"
    };

    private static readonly string[] ProgressionColumns =
    {
        "event_time", "user_id", "progress_type", "subject_id", "change_amount", "before_value", "after_value", "action_code", "ref_id"
    };

    public ClickHouseGameEventLogger(
        ILogger<ClickHouseGameEventLogger> logger,
        IOptions<GameEventLogConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;

        _client = new ClickHouseClient(_config.ClickHouse);

        var economyChannelOptions = new BoundedChannelOptions(_config.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        };
        _economyChannel = Channel.CreateBounded<EconomyEvent>(economyChannelOptions);

        var missionBattleChannelOptions = new BoundedChannelOptions(_config.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        };
        _missionBattleChannel = Channel.CreateBounded<MissionBattleEvent>(missionBattleChannelOptions);

        var progressionChannelOptions = new BoundedChannelOptions(_config.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        };
        _progressionChannel = Channel.CreateBounded<ProgressionEvent>(progressionChannelOptions);
    }

    public void LogEconomyEvent(
        long userId,
        string assetType,
        int assetId,
        long changeAmount,
        long beforeAmount,
        long afterAmount,
        string actionCode,
        string? refId)
    {
        try
        {
            var evt = new EconomyEvent(
                DateTime.UtcNow,
                userId,
                assetType,
                assetId,
                changeAmount,
                beforeAmount,
                afterAmount,
                actionCode,
                refId ?? string.Empty
            );

            if (!_economyChannel.Writer.TryWrite(evt))
            {
                _logger.LogWarning("ClickHouse economy event channel is full. Event dropped. UserId: {UserId}, Action: {Action}", userId, actionCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing economy event. UserId: {UserId}", userId);
        }
    }

    public void LogMissionBattleEvent(
        long userId,
        string? ticketId,
        int missionId,
        int characterId,
        long seed,
        MissionOutcome outcome,
        IReadOnlyList<BattleLogEntry> logs)
    {
        try
        {
            var stageCount = (ushort)logs.Count;
            var stageIndex = new int[stageCount];
            var stageName = new string[stageCount];
            var stageStat = new string[stageCount];
            var stageRoll = new int[stageCount];
            var stageRequired = new int[stageCount];
            var stagePassed = new byte[stageCount];
            var stageMentalAfter = new int[stageCount];

            for (int i = 0; i < stageCount; i++)
            {
                var log = logs[i];
                stageIndex[i] = log.Index;
                stageName[i] = log.Stage;
                stageStat[i] = log.Stat.ToString();
                stageRoll[i] = log.Roll;
                stageRequired[i] = log.Required;
                stagePassed[i] = log.Passed
                    ? (byte)1
                    : (byte)0;
                stageMentalAfter[i] = log.MentalAfter;
            }

            var evt = new MissionBattleEvent(
                DateTime.UtcNow,
                userId,
                ticketId ?? string.Empty,
                missionId,
                characterId,
                seed,
                outcome.ToString(),
                stageCount,
                stageIndex,
                stageName,
                stageStat,
                stageRoll,
                stageRequired,
                stagePassed,
                stageMentalAfter
            );

            if (!_missionBattleChannel.Writer.TryWrite(evt))
            {
                _logger.LogWarning("ClickHouse mission battle event channel is full. Event dropped. UserId: {UserId}, TicketId: {TicketId}", userId, ticketId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing mission battle event. UserId: {UserId}", userId);
        }
    }

    public void LogProgressionEvent(
        long userId,
        string progressType,
        int subjectId,
        long changeAmount,
        long beforeValue,
        long afterValue,
        string actionCode,
        string? refId)
    {
        try
        {
            var evt = new ProgressionEvent(
                DateTime.UtcNow,
                userId,
                progressType,
                subjectId,
                changeAmount,
                beforeValue,
                afterValue,
                actionCode,
                refId ?? string.Empty
            );

            if (!_progressionChannel.Writer.TryWrite(evt))
            {
                _logger.LogWarning("ClickHouse progression event channel is full. Event dropped. UserId: {UserId}, Action: {Action}", userId, actionCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing progression event. UserId: {UserId}", userId);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.FlushIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await FlushAllAsync();
        }

        _logger.LogInformation("ClickHouseGameEventLogger is stopping. Performing best-effort final flush.");
        await FlushAllAsync();
    }

    private async Task FlushAllAsync()
    {
        try
        {
            await FlushChannelAsync(
                "rtw_game_log.economy_event",
                _economyChannel,
                EconomyColumns,
                ev => new object[]
                {
                    ev.EventTime,
                    ev.UserId,
                    ev.AssetType,
                    ev.AssetId,
                    ev.ChangeAmount,
                    ev.BeforeAmount,
                    ev.AfterAmount,
                    ev.ActionCode,
                    ev.RefId
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error flushing economy events.");
        }

        try
        {
            await FlushChannelAsync(
                "rtw_game_log.mission_battle_event",
                _missionBattleChannel,
                MissionBattleColumns,
                ev => new object[]
                {
                    ev.EventTime,
                    ev.UserId,
                    ev.TicketId,
                    ev.MissionId,
                    ev.CharacterId,
                    ev.Seed,
                    ev.Outcome,
                    ev.StageCount,
                    ev.StageIndex,
                    ev.StageName,
                    ev.StageStat,
                    ev.StageRoll,
                    ev.StageRequired,
                    ev.StagePassed,
                    ev.StageMentalAfter
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error flushing mission battle events.");
        }

        try
        {
            await FlushChannelAsync(
                "rtw_game_log.progression_event",
                _progressionChannel,
                ProgressionColumns,
                ev => new object[]
                {
                    ev.EventTime,
                    ev.UserId,
                    ev.ProgressType,
                    ev.SubjectId,
                    ev.ChangeAmount,
                    ev.BeforeValue,
                    ev.AfterValue,
                    ev.ActionCode,
                    ev.RefId
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error flushing progression events.");
        }
    }

    private async Task FlushChannelAsync<T>(
        string tableName,
        Channel<T> channel,
        string[] columns,
        Func<T, object[]> mapper)
    {
        var batch = DrainChannel(channel, _config.BatchSize);
        if (batch.Count == 0) return;

        try
        {
            await InsertWithRetryAsync(tableName, batch, columns, mapper);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to flush events to table {Table} after retry. Events dropped.", tableName);
        }
    }

    private List<T> DrainChannel<T>(Channel<T> channel, int batchSize)
    {
        var list = new List<T>();
        var reader = channel.Reader;
        while (list.Count < batchSize && reader.TryRead(out var item))
        {
            list.Add(item);
        }

        return list;
    }

    private async Task InsertWithRetryAsync<T>(
        string tableName,
        List<T> batch,
        string[] columns,
        Func<T, object[]> mapper)
    {
        // Try 1
        try
        {
            if (!_schemaReady)
            {
                await EnsureSchemaAsync();
            }

            await DoInsertAsync(tableName, batch, columns, mapper);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickHouse insert failed on first attempt for {Table}. Resetting schema flag and retrying...", tableName);
            _schemaReady = false;
        }

        // Retry (Try 2)
        try
        {
            if (!_schemaReady)
            {
                await EnsureSchemaAsync();
            }

            await DoInsertAsync(tableName, batch, columns, mapper);
        }
        catch (Exception ex)
        {
            throw new Exception($"Retry failed for {tableName}", ex);
        }
    }

    private async Task DoInsertAsync<T>(
        string tableName,
        List<T> batch,
        string[] columns,
        Func<T, object[]> mapper)
    {
        var rows = batch.Select(mapper).ToList();
        await _client.InsertBinaryAsync(tableName, columns, rows);
    }

    private async Task EnsureSchemaAsync()
    {
        await _schemaLock.WaitAsync();
        try
        {
            if (_schemaReady) return;

            _logger.LogInformation("DDL Bootstrap: Ensuring database and tables exist in ClickHouse.");

            await _client.ExecuteNonQueryAsync("CREATE DATABASE IF NOT EXISTS rtw_game_log");

            await _client.ExecuteNonQueryAsync(@"
CREATE TABLE IF NOT EXISTS rtw_game_log.economy_event
(
    event_time    DateTime64(3),
    user_id       Int64,
    asset_type    LowCardinality(String),
    asset_id      Int32,
    change_amount Int64,
    before_amount Int64,
    after_amount  Int64,
    action_code   LowCardinality(String),
    ref_id        String
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(event_time)
ORDER BY (user_id, event_time)");

            await _client.ExecuteNonQueryAsync(@"
CREATE TABLE IF NOT EXISTS rtw_game_log.mission_battle_event
(
    event_time         DateTime64(3),
    user_id            Int64,
    ticket_id          String,
    mission_id         Int32,
    character_id       Int32,
    seed               Int64,
    outcome            LowCardinality(String),
    stage_count        UInt16,
    stage_index        Array(Int32),
    stage_name         Array(LowCardinality(String)),
    stage_stat         Array(LowCardinality(String)),
    stage_roll         Array(Int32),
    stage_required     Array(Int32),
    stage_passed       Array(UInt8),
    stage_mental_after Array(Int32)
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(event_time)
ORDER BY (user_id, event_time)");

            await _client.ExecuteNonQueryAsync(@"
CREATE TABLE IF NOT EXISTS rtw_game_log.progression_event
(
    event_time    DateTime64(3),
    user_id       Int64,
    progress_type LowCardinality(String),
    subject_id    Int32,
    change_amount Int64,
    before_value  Int64,
    after_value   Int64,
    action_code   LowCardinality(String),
    ref_id        String
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(event_time)
ORDER BY (user_id, event_time)");

            _schemaReady = true;
            _logger.LogInformation("DDL Bootstrap: ClickHouse schema ensured successfully.");
        }
        finally
        {
            _schemaLock.Release();
        }
    }

    public override void Dispose()
    {
        _client.Dispose();
        _schemaLock.Dispose();
        base.Dispose();
    }
}
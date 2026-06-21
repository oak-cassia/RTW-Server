using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.Exceptions;
using RTWWebServer.Game.Mission;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Providers.MasterData;

namespace RTWWebServer.Services;

public class MissionService(
    IUserRepository userRepository,
    IPlayerCharacterRepository playerCharacterRepository,
    IMasterDataProvider masterDataProvider,
    IMissionBattleSimulator battleSimulator,
    IDistributedCacheAdapter cache,
    IRemoteCacheKeyGenerator keyGenerator,
    IGuidGenerator guidGenerator,
    ILogger<MissionService> logger
) : IMissionService
{
    // 티켓/결과는 한 판의 수명 동안만 유효하면 된다. 미정산 시 TTL로 자동 소멸한다.
    private static readonly TimeSpan TicketExpiration = TimeSpan.FromMinutes(30);

    public async Task<MissionTicketDto> StartMissionAsync(long userId, int missionId, int characterId)
    {
        if (!masterDataProvider.TryGetMission(missionId, out var mission))
        {
            throw new GameException($"Mission not found: {missionId}", WebServerErrorCode.MissionNotFound);
        }

        // 투입 캐릭터는 클라가 지정하므로 반드시 소유를 검증한다(미보유 캐릭터 투입 차단).
        // (UserId, CharacterMasterId)는 유니크하므로 존재하면 보유로 간주한다.
        if (await playerCharacterRepository.GetByUserIdAndCharacterMasterIdAsync(userId, characterId) == null)
        {
            throw new GameException($"Character not owned: {characterId}", WebServerErrorCode.CharacterNotOwned);
        }

        // 전투에 투입할 캐릭터 스탯(D7: Slice 1은 캐릭터 마스터의 베이스 스탯만 사용).
        if (!masterDataProvider.TryGetCharacter(characterId, out var character))
        {
            throw new GameException($"Character master not found: {characterId}", WebServerErrorCode.InvalidArgument);
        }

        // 입장 비용(스태미나)을 먼저 원자 차감한다. 실패하면 티켓을 만들지 않는다.
        // 차감이 성공한 뒤 티켓/결과 기록(Redis)이 실패하면 스태미나가 소모된 채 남을 수 있으나,
        // 인프라 장애 시의 드문 경우다(엄밀한 보장은 outbox 등이 필요 — 스켈레톤 범위 밖).
        if (await userRepository.TryConsumeStaminaAsync(userId, mission.StaminaCost) == false)
        {
            throw new GameException("Insufficient stamina", WebServerErrorCode.InsufficientStamina);
        }

        // 서버 생성 시드 + 티켓 ID. 결과는 이 시드로만 결정된다(클라가 개입 불가).
        long seed = Random.Shared.NextInt64();
        string ticketId = guidGenerator.GenerateGuid().ToString();

        var ticket = new MissionTicket
        {
            TicketId = ticketId,
            UserId = userId,
            MissionId = missionId,
            Seed = seed
        };
        await cache.SetAsync(keyGenerator.GenerateMissionTicketKey(ticketId), ticket, TicketExpiration);

        // ───── 임시(스텁): 실시간 게임서버가 생기기 전까지 웹서버가 게임서버 역할을 대신한다. ─────
        // 실제 흐름에선 클라가 게임서버에서 전투를 끝내면 게임서버가 mission:result 키를 Redis에 기록한다.
        // 게임서버 도입 시 이 블록을 통째로 삭제하면 된다(start는 티켓 발급까지만 수행).
        var battle = battleSimulator.Simulate(character, mission, seed);
        await cache.SetAsync(keyGenerator.GenerateMissionResultKey(ticketId), ToRunResult(battle), TicketExpiration);
        // ───────────────────────────────────────────────────────────────────────────────

        logger.LogInformation(
            "Mission {MissionId} reserved for userId {UserId}, ticket {TicketId}", missionId, userId, ticketId);

        return new MissionTicketDto { TicketId = ticketId, Seed = seed };
    }

    public async Task<MissionResultDto> CompleteMissionAsync(long userId, string ticketId)
    {
        string ticketKey = keyGenerator.GenerateMissionTicketKey(ticketId);
        string resultKey = keyGenerator.GenerateMissionResultKey(ticketId);
        string lockKey = keyGenerator.GenerateMissionSettleLockKey(ticketId);
        string lockValue = guidGenerator.GenerateGuid().ToString();

        // 동시 정산을 직렬화한다. 락이 없으면 두 요청이 같은 티켓으로 보상을 중복 지급할 수 있다.
        if (await cache.LockAsync(lockKey, lockValue) == false)
        {
            throw new GameException($"Failed to lock mission settle: {ticketId}", WebServerErrorCode.RemoteCacheLockFailed);
        }

        long fameGained;
        long goldGained;
        MissionRunResult runResult;
        try
        {
            var ticket = await cache.GetAsync<MissionTicket>(ticketKey);
            // 소유자 불일치도 '없음'으로 취급한다(티켓 존재 여부를 노출하지 않음).
            if (ticket == null || ticket.UserId != userId)
            {
                throw new GameException($"Mission ticket not found: {ticketId}", WebServerErrorCode.MissionTicketNotFound);
            }

            // 결과가 아직 없으면 게임서버가 전투를 끝내지 않은 것 — 잠시 후 재시도.
            runResult = await cache.GetAsync<MissionRunResult>(resultKey)
                ?? throw new GameException($"Mission result not ready: {ticketId}", WebServerErrorCode.MissionResultNotReady);

            // 보상 금액은 신뢰할 수 없는 입력에서 받지 않고, 티켓의 MissionId로 마스터에서 재계산한다.
            if (!masterDataProvider.TryGetMission(ticket.MissionId, out var mission))
            {
                throw new GameException($"Mission not found: {ticket.MissionId}", WebServerErrorCode.MissionNotFound);
            }

            // D6: 보상은 승리 시에만. 탈락이면 (입장 시 차감된) 스태미나만 소모되고 보상은 0.
            bool won = runResult.Outcome == MissionOutcome.Win;
            fameGained = won ? mission.RewardFame : 0;
            goldGained = won ? mission.RewardGold : 0;

            if (fameGained > 0 || goldGained > 0)
            {
                await userRepository.ApplyMissionRewardsAsync(userId, fameGained, goldGained);
            }

            // 정산 완료 표시: 티켓/결과를 삭제 → 재호출 시 '없음'이 되어 중복 정산되지 않는다.
            // (지급 성공 후 삭제 사이에 프로세스가 죽는 극히 드문 창은 남는다 — 엄밀한 exactly-once는
            //  DB 멱등키/outbox가 필요하며 스켈레톤 범위 밖.)
            await cache.RemoveAsync(ticketKey);
            await cache.RemoveAsync(resultKey);
        }
        finally
        {
            await cache.UnlockAsync(lockKey, lockValue);
        }

        // ExecuteUpdate는 체인지 트래커를 우회하므로, 응답용 최신 상태는 지급 후 새로 읽는다.
        var updatedUser = await userRepository.GetByIdAsNoTrackingAsync(userId)
            ?? throw new GameException("User not found", WebServerErrorCode.UserNotFound);

        return new MissionResultDto
        {
            Outcome = runResult.Outcome,
            Log = runResult.Log,
            FameGained = fameGained,
            GoldGained = goldGained,
            NewFame = updatedUser.Fame,
            NewGold = updatedUser.FreeCurrency,
            NewStamina = updatedUser.CurrentStamina,
            Seed = runResult.Seed
        };
    }

    // 도메인 전투 결과 → Redis 결과 표현. start의 스텁이 게임서버 대신 결과를 기록할 때 쓴다.
    private static MissionRunResult ToRunResult(BattleResult battle) => new()
    {
        Outcome = battle.Outcome,
        Seed = battle.Seed,
        Log = battle.Log.Select(e => new BattleLogEntryDto
        {
            Index = e.Index,
            Stage = e.Stage,
            Stat = e.Stat,
            Roll = e.Roll,
            Required = e.Required,
            Passed = e.Passed,
            MentalAfter = e.MentalAfter,
            Message = e.Message
        }).ToArray()
    };
}

using Microsoft.Extensions.Logging;
using Moq;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.Exceptions;
using RTWWebServer.Game.Mission;
using RTWWebServer.MasterDatas.Models;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Providers.MasterData;
using RTWWebServer.Services;

namespace RTWTest.Webserver.Services;

[TestFixture]
public class MissionServiceTests
{
    private static readonly Guid FixedGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IPlayerCharacterRepository> _mockPlayerCharacterRepository = null!;
    private Mock<IMasterDataProvider> _mockMasterDataProvider = null!;
    private Mock<IDistributedCacheAdapter> _mockCache = null!;
    private IRemoteCacheKeyGenerator _keyGenerator = null!;
    private Mock<IGuidGenerator> _mockGuidGenerator = null!;
    private MissionService _service = null!;

    private string TicketId => FixedGuid.ToString();
    private string TicketKey => _keyGenerator.GenerateMissionTicketKey(TicketId);
    private string ResultKey => _keyGenerator.GenerateMissionResultKey(TicketId);

    [SetUp]
    public void SetUp()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPlayerCharacterRepository = new Mock<IPlayerCharacterRepository>();
        _mockMasterDataProvider = new Mock<IMasterDataProvider>();
        _mockCache = new Mock<IDistributedCacheAdapter>();
        _keyGenerator = new RemoteCacheKeyGenerator();
        _mockGuidGenerator = new Mock<IGuidGenerator>();
        _mockGuidGenerator.Setup(x => x.GenerateGuid()).Returns(FixedGuid);

        // 정산 락은 기본 성공. 락 실패 케이스에서만 재정의한다.
        _mockCache.Setup(x => x.LockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockCache.Setup(x => x.UnlockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // 실제 스텁 시뮬레이터 사용(순수 함수) — 항상 승리.
        _service = new MissionService(
            _mockUserRepository.Object,
            _mockPlayerCharacterRepository.Object,
            _mockMasterDataProvider.Object,
            new MissionBattleSimulator(),
            _mockCache.Object,
            _keyGenerator,
            _mockGuidGenerator.Object,
            Mock.Of<ILogger<MissionService>>());
    }

    // ───────────────────────── start (예약) ─────────────────────────

    [Test]
    public void StartMissionAsync_MissionNotFound_ThrowsGameException()
    {
        MissionMaster outMission = null!;
        _mockMasterDataProvider.Setup(x => x.TryGetMission(It.IsAny<int>(), out outMission)).Returns(false);

        var ex = Assert.ThrowsAsync<GameException>(async () => await _service.StartMissionAsync(1, 999, 1));

        Assert.That(ex!.ErrorCode, Is.EqualTo(WebServerErrorCode.MissionNotFound));
        _mockUserRepository.Verify(
            x => x.TryConsumeStaminaAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void StartMissionAsync_InsufficientStamina_ThrowsAndWritesNoTicket()
    {
        const long userId = 1;
        var mission = NewMission();
        var character = NewCharacter();

        _mockMasterDataProvider.Setup(x => x.TryGetMission(mission.Id, out mission)).Returns(true);
        SetupOwnedCharacter(userId, character);
        _mockUserRepository.Setup(x => x.TryConsumeStaminaAsync(userId, mission.StaminaCost, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<GameException>(async () => await _service.StartMissionAsync(userId, mission.Id, character.Id));

        Assert.That(ex!.ErrorCode, Is.EqualTo(WebServerErrorCode.InsufficientStamina));
        // 차감 실패 시 티켓/결과를 Redis에 쓰지 않는다.
        _mockCache.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task StartMissionAsync_Success_DeductsStamina_WritesTicketAndResult_ReturnsTicket()
    {
        const long userId = 1;
        var mission = NewMission();
        var character = NewCharacter();

        _mockMasterDataProvider.Setup(x => x.TryGetMission(mission.Id, out mission)).Returns(true);
        SetupOwnedCharacter(userId, character);
        _mockUserRepository.Setup(x => x.TryConsumeStaminaAsync(userId, mission.StaminaCost, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ticket = await _service.StartMissionAsync(userId, mission.Id, character.Id);

        Assert.That(ticket.TicketId, Is.EqualTo(TicketId));
        // 티켓과 (스텁) 결과가 각각 기록된다.
        _mockCache.Verify(
            x => x.SetAsync(TicketKey, It.IsAny<MissionTicket>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockCache.Verify(
            x => x.SetAsync(ResultKey, It.IsAny<MissionRunResult>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // 보상은 start가 아니라 end에서 지급된다.
        _mockUserRepository.Verify(
            x => x.ApplyMissionRewardsAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void StartMissionAsync_CharacterNotOwned_ThrowsAndWritesNoTicket()
    {
        const long userId = 1;
        var mission = NewMission();
        var character = NewCharacter();

        _mockMasterDataProvider.Setup(x => x.TryGetMission(mission.Id, out mission)).Returns(true);
        // 유저가 보유하지 않은 캐릭터 → 소유 조회가 null.
        _mockPlayerCharacterRepository
            .Setup(x => x.GetByUserIdAndCharacterMasterIdAsync(userId, character.Id))
            .ReturnsAsync((PlayerCharacter?)null);

        var ex = Assert.ThrowsAsync<GameException>(
            async () => await _service.StartMissionAsync(userId, mission.Id, character.Id));

        Assert.That(ex!.ErrorCode, Is.EqualTo(WebServerErrorCode.CharacterNotOwned));
        // 소유 검증 실패 시 스태미나를 차감하지 않고 티켓도 쓰지 않는다.
        _mockUserRepository.Verify(
            x => x.TryConsumeStaminaAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockCache.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────────── end (정산) ─────────────────────────

    [Test]
    public async Task CompleteMissionAsync_Win_AppliesRewards_RemovesKeys_ReturnsUpdatedState()
    {
        const long userId = 1;
        var mission = NewMission();
        SetupTicket(userId, mission.Id);
        SetupResult(MissionOutcome.Win);
        _mockMasterDataProvider.Setup(x => x.TryGetMission(mission.Id, out mission)).Returns(true);
        // 정산 후 재조회가 반환할 "보상 반영 후" 상태.
        _mockUserRepository.Setup(x => x.GetByIdAsNoTrackingAsync(userId))
            .ReturnsAsync(NewUser(userId, fame: 120, gold: 500, stamina: 95));

        var result = await _service.CompleteMissionAsync(userId, TicketId);

        Assert.That(result.Outcome, Is.EqualTo(MissionOutcome.Win));
        Assert.That(result.FameGained, Is.EqualTo(mission.RewardFame));
        Assert.That(result.GoldGained, Is.EqualTo(mission.RewardGold));
        Assert.That(result.NewFame, Is.EqualTo(120));
        Assert.That(result.NewGold, Is.EqualTo(500));
        Assert.That(result.NewStamina, Is.EqualTo(95));

        _mockUserRepository.Verify(
            x => x.ApplyMissionRewardsAsync(userId, mission.RewardFame, mission.RewardGold, It.IsAny<CancellationToken>()),
            Times.Once);
        // 멱등: 정산 후 티켓/결과를 삭제한다.
        _mockCache.Verify(x => x.RemoveAsync(TicketKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync(ResultKey, It.IsAny<CancellationToken>()), Times.Once);
        // 락은 반드시 해제된다.
        _mockCache.Verify(x => x.UnlockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task CompleteMissionAsync_Lose_NoRewardApplied()
    {
        const long userId = 1;
        var mission = NewMission();
        SetupTicket(userId, mission.Id);
        SetupResult(MissionOutcome.Lose);
        _mockMasterDataProvider.Setup(x => x.TryGetMission(mission.Id, out mission)).Returns(true);
        _mockUserRepository.Setup(x => x.GetByIdAsNoTrackingAsync(userId))
            .ReturnsAsync(NewUser(userId, fame: 0, gold: 0, stamina: 95));

        var result = await _service.CompleteMissionAsync(userId, TicketId);

        Assert.That(result.Outcome, Is.EqualTo(MissionOutcome.Lose));
        Assert.That(result.FameGained, Is.EqualTo(0));
        Assert.That(result.GoldGained, Is.EqualTo(0));
        _mockUserRepository.Verify(
            x => x.ApplyMissionRewardsAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void CompleteMissionAsync_TicketNotFound_ThrowsAndReleasesLock()
    {
        const long userId = 1;
        _mockCache.Setup(x => x.GetAsync<MissionTicket>(TicketKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionTicket?)null);

        var ex = Assert.ThrowsAsync<GameException>(async () => await _service.CompleteMissionAsync(userId, TicketId));

        Assert.That(ex!.ErrorCode, Is.EqualTo(WebServerErrorCode.MissionTicketNotFound));
        _mockCache.Verify(x => x.UnlockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void CompleteMissionAsync_OwnerMismatch_ThrowsTicketNotFound()
    {
        const long userId = 1;
        // 티켓 소유자는 다른 유저(2).
        SetupTicket(ownerUserId: 2, missionId: 101);

        var ex = Assert.ThrowsAsync<GameException>(async () => await _service.CompleteMissionAsync(userId, TicketId));

        Assert.That(ex!.ErrorCode, Is.EqualTo(WebServerErrorCode.MissionTicketNotFound));
        _mockUserRepository.Verify(
            x => x.ApplyMissionRewardsAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void CompleteMissionAsync_ResultNotReady_Throws()
    {
        const long userId = 1;
        SetupTicket(userId, missionId: 101);
        _mockCache.Setup(x => x.GetAsync<MissionRunResult>(ResultKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionRunResult?)null);

        var ex = Assert.ThrowsAsync<GameException>(async () => await _service.CompleteMissionAsync(userId, TicketId));

        Assert.That(ex!.ErrorCode, Is.EqualTo(WebServerErrorCode.MissionResultNotReady));
    }

    [Test]
    public void CompleteMissionAsync_LockFailed_Throws()
    {
        const long userId = 1;
        _mockCache.Setup(x => x.LockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<GameException>(async () => await _service.CompleteMissionAsync(userId, TicketId));

        Assert.That(ex!.ErrorCode, Is.EqualTo(WebServerErrorCode.RemoteCacheLockFailed));
    }

    // ───────────────────────── helpers ─────────────────────────

    private void SetupTicket(long ownerUserId, int missionId)
    {
        _mockCache.Setup(x => x.GetAsync<MissionTicket>(TicketKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MissionTicket { TicketId = TicketId, UserId = ownerUserId, MissionId = missionId, Seed = 42 });
    }

    private void SetupResult(MissionOutcome outcome)
    {
        _mockCache.Setup(x => x.GetAsync<MissionRunResult>(ResultKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MissionRunResult
            {
                Outcome = outcome,
                Seed = 42,
                Log = [new BattleLogEntryDto { Index = 0, Message = "test" }]
            });
    }

    // 캐릭터를 보유한 상태로 만든다: 마스터 조회 성공 + 소유 조회가 PlayerCharacter를 반환.
    private void SetupOwnedCharacter(long userId, CharacterMaster character)
    {
        _mockMasterDataProvider.Setup(x => x.TryGetCharacter(character.Id, out character)).Returns(true);
        _mockPlayerCharacterRepository
            .Setup(x => x.GetByUserIdAndCharacterMasterIdAsync(userId, character.Id))
            .ReturnsAsync(new PlayerCharacter(userId, character.Id, level: 1, currentExp: 0, obtainedAt: DateTime.UtcNow));
    }

    private static MissionMaster NewMission() =>
        new() { Id = 101, Name = "테스트 임무", StaminaCost = 5, StartingMental = 100, RewardFame = 120, RewardGold = 500 };

    private static CharacterMaster NewCharacter() =>
        new() { Id = 1, Name = "유우", Portfolio = 15, Development = 25, JobSearching = 30 };

    private static User NewUser(long id, long fame, long gold, int stamina) =>
        new User(
            accountId: id,
            nickname: "TestUser",
            level: 1,
            currentExp: 0,
            currentStamina: stamina,
            maxStamina: 100,
            lastStaminaRecharge: DateTime.UtcNow,
            premiumCurrency: 0,
            freeCurrency: gold,
            mainCharacterId: 0, // 프로필 아바타. 임무 동작과 무관하므로 테스트에선 의미 없음.
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow)
        { Id = id, Fame = fame };
}

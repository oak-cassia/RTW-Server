using System.Collections.Immutable;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Cache;
using RTWWebServer.Data;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.Exceptions;
using RTWWebServer.MasterDatas.Models;
using RTWWebServer.Providers.MasterData;
using RTWWebServer.Services;

namespace RTWTest.Webserver.Services;

// P1(조건부 UPDATE 차감)·트랜잭션 회귀 가드. EF InMemory는 ExecuteUpdateAsync·트랜잭션을 지원하지
// 않으므로, 관계형 SQLite(in-memory)에 실제 리포지토리를 묶어 차감 원자성·트래커 우회를 검증한다.
// (MySqlException 기반 중복키 캐치 경로는 MySQL 전용이라 SQLite로는 재현하지 않는다.)
[TestFixture]
public class CharacterGachaServiceIntegrationTests
{
    private SqliteConnection _connection;
    private GameDbContext _dbContext;
    private UserRepository _userRepository;
    private PlayerCharacterRepository _playerCharacterRepository;
    private Mock<IMasterDataProvider> _mockMasterDataProvider;
    private Mock<IPlayerCharacterCache> _mockPlayerCharacterCache;
    private CharacterGachaService _service;

    [SetUp]
    public void SetUp()
    {
        // in-memory SQLite는 연결이 닫히면 사라지므로 연결을 테스트 수명 동안 열어 둔다.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new GameDbContext(options);
        _dbContext.Database.EnsureCreated();

        // 서비스의 명시적 트랜잭션이 리포지토리 작업까지 묶으려면 같은 컨텍스트 인스턴스를 공유해야 한다.
        _userRepository = new UserRepository(_dbContext);
        _playerCharacterRepository = new PlayerCharacterRepository(_dbContext);

        _mockMasterDataProvider = new Mock<IMasterDataProvider>();
        _mockPlayerCharacterCache = new Mock<IPlayerCharacterCache>();
        _mockPlayerCharacterCache
            .Setup(x => x.GetAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<PlayerCharacter>?)null);
        _mockPlayerCharacterCache
            .Setup(x => x.SetAsync(It.IsAny<long>(), It.IsAny<List<PlayerCharacter>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockPlayerCharacterCache
            .Setup(x => x.InvalidateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new CharacterGachaService(
            _dbContext,
            _userRepository,
            _playerCharacterRepository,
            _mockMasterDataProvider.Object,
            _mockPlayerCharacterCache.Object,
            Mock.Of<ILogger<CharacterGachaService>>());
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }

    [Test]
    public async Task PerformGachaAsync_DeductsCurrencyAtomically_AndPersistsCharacters()
    {
        // Arrange: 잔액 500, 마스터 3종. count=1(비용 300) → 200 남아야 한다.
        var userId = SeedUser(premiumCurrency: 500, freeCurrency: 50);
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters()).Returns(BuildCharacters(3));

        // Act
        var result = await _service.PerformGachaAsync(userId, 1, 1);

        // Assert: 응답 잔액은 트래커가 아니라 커밋된 DB 상태를 반영한다.
        Assert.That(result.CharacterMasterIds, Has.Count.EqualTo(1));
        Assert.That(result.RemainingPremiumCurrency, Is.EqualTo(200));
        Assert.That(result.RemainingFreeCurrency, Is.EqualTo(50));

        // DB에 실제로 차감되고 캐릭터가 적재됐는지 새 컨텍스트로 확인한다.
        await using var verifyContext = CreateVerifyContext();
        var persistedUser = await verifyContext.Users.SingleAsync(u => u.Id == userId);
        Assert.That(persistedUser.PremiumCurrency, Is.EqualTo(200));

        var persistedCharacters = await verifyContext.PlayerCharacters.Where(pc => pc.UserId == userId).ToListAsync();
        Assert.That(persistedCharacters, Has.Count.EqualTo(1));
        Assert.That(persistedCharacters[0].CharacterMasterId, Is.EqualTo(result.CharacterMasterIds[0]));
    }

    [Test]
    public async Task PerformGachaAsync_InsufficientBalance_DoesNotDeductOrInsert()
    {
        // Arrange: 잔액 200 < 비용 300. 조건부 UPDATE가 0행을 영향 → 차감·지급 모두 없어야 한다.
        var userId = SeedUser(premiumCurrency: 200, freeCurrency: 0);
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters()).Returns(BuildCharacters(3));

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.PerformGachaAsync(userId, 1, 1));
        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InsufficientCurrency));

        await using var verifyContext = CreateVerifyContext();
        var persistedUser = await verifyContext.Users.SingleAsync(u => u.Id == userId);
        Assert.That(persistedUser.PremiumCurrency, Is.EqualTo(200));
        Assert.That(await verifyContext.PlayerCharacters.AnyAsync(pc => pc.UserId == userId), Is.False);
    }

    [Test]
    public async Task TryDeductPremiumCurrencyAsync_BypassesChangeTracker()
    {
        // ExecuteUpdateAsync는 SQL을 직접 실행하고 체인지 트래커를 우회한다. 이미 추적 중이던
        // 엔티티는 stale 상태로 남으므로, 서비스가 커밋 후 AsNoTracking으로 재조회하는 이유를 문서화한다.
        var userId = SeedUser(premiumCurrency: 500, freeCurrency: 0);

        // 추적되는 인스턴스를 먼저 적재(잔액 500)
        var tracked = await _userRepository.GetByIdAsync(userId);
        Assert.That(tracked!.PremiumCurrency, Is.EqualTo(500));

        var deducted = await _userRepository.TryDeductPremiumCurrencyAsync(userId, 300);
        Assert.That(deducted, Is.True);

        // 추적 인스턴스는 여전히 stale(500), 새로 읽으면 차감 반영(200).
        Assert.That(tracked.PremiumCurrency, Is.EqualTo(500));
        var fresh = await _userRepository.GetByIdAsNoTrackingAsync(userId);
        Assert.That(fresh!.PremiumCurrency, Is.EqualTo(200));
    }

    private long SeedUser(long premiumCurrency, long freeCurrency)
    {
        var user = new User(
            accountId: Random.Shared.NextInt64(1, long.MaxValue),
            nickname: $"u_{Guid.NewGuid():N}".Substring(0, 12),
            level: 1,
            currentExp: 0,
            currentStamina: 100,
            maxStamina: 100,
            lastStaminaRecharge: DateTime.UtcNow,
            premiumCurrency: premiumCurrency,
            freeCurrency: freeCurrency,
            mainCharacterId: 1,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow);

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        // 시드로 추적된 엔티티가 이후 서비스 흐름에 끼어들지 않도록 트래커를 비운다(요청 경계 모사).
        _dbContext.ChangeTracker.Clear();
        return user.Id;
    }

    // 커밋된 상태를 추적 없이 검증하기 위한 별도 컨텍스트(같은 in-memory DB 연결 공유).
    private GameDbContext CreateVerifyContext()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new GameDbContext(options);
    }

    private static ImmutableDictionary<int, CharacterMaster> BuildCharacters(int count)
    {
        var builder = ImmutableDictionary.CreateBuilder<int, CharacterMaster>();
        for (var id = 1; id <= count; id++)
        {
            builder[id] = new CharacterMaster { Id = id, Name = $"Char{id}", Portfolio = id, Development = id, JobSearching = id };
        }

        return builder.ToImmutable();
    }
}

using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

[TestFixture]
public class CharacterGachaServiceTests
{
    private GameDbContext _dbContext;
    private Mock<IMasterDataProvider> _mockMasterDataProvider;
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IPlayerCharacterRepository> _mockPlayerCharacterRepository;
    private Mock<IPlayerCharacterCache> _mockPlayerCharacterCache;
    private CharacterGachaService _service;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            // InMemory는 트랜잭션을 지원하지 않으므로 서비스의 BeginTransaction을 무시(no-op) 처리한다.
            // 차감+INSERT의 실제 원자성은 P4의 관계형 DB 통합 테스트에서 검증한다.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new GameDbContext(options);
        
        _mockMasterDataProvider = new Mock<IMasterDataProvider>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPlayerCharacterRepository = new Mock<IPlayerCharacterRepository>();
        _mockPlayerCharacterCache = new Mock<IPlayerCharacterCache>();

        // 캐시 조회는 기본적으로 미스 처리하여 리포지토리 경로를 타도록 설정
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
            _mockUserRepository.Object,
            _mockPlayerCharacterRepository.Object,
            _mockMasterDataProvider.Object,
            _mockPlayerCharacterCache.Object,
            Mock.Of<ILogger<CharacterGachaService>>());
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
    }

    [Test]
    public void PerformGachaAsync_UserMissingAfterCommit_ThrowsGameException()
    {
        // 차감은 성공했으나 커밋 후 응답용 재조회에서 유저가 사라진(사실상 발생 불가) 방어 경로
        const long userId = 1;
        var allCharacters = new Dictionary<int, CharacterMaster>
        {
            { 1, new CharacterMaster { Id = 1, Name = "Char1", Portfolio = 10, Development = 10, JobSearching = 10 } },
            { 2, new CharacterMaster { Id = 2, Name = "Char2", Portfolio = 20, Development = 20, JobSearching = 20 } },
        }.ToImmutableDictionary();

        _mockPlayerCharacterRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<PlayerCharacter>());
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters())
            .Returns(allCharacters);
        _mockUserRepository.Setup(x => x.TryDeductPremiumCurrencyAsync(userId, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(x => x.GetByIdAsNoTrackingAsync(userId))
            .ReturnsAsync(null as User);

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.PerformGachaAsync(userId, 1, 1));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.UserNotFound));
    }

    [Test]
    public void PerformGachaAsync_InsufficientCurrency_ThrowsGameException()
    {
        // Arrange
        const long userId = 1;
        const int count = 2;

        var allCharacters = new Dictionary<int, CharacterMaster>
        {
            { 1, new CharacterMaster { Id = 1, Name = "Char1", Portfolio = 10, Development = 10, JobSearching = 10 } },
            { 2, new CharacterMaster { Id = 2, Name = "Char2", Portfolio = 20, Development = 20, JobSearching = 20 } },
        }.ToImmutableDictionary();

        _mockPlayerCharacterRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<PlayerCharacter>());
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters())
            .Returns(allCharacters);
        // 잔액 부족 시 조건부 UPDATE가 0행을 영향 → false
        _mockUserRepository.Setup(x => x.TryDeductPremiumCurrencyAsync(userId, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.PerformGachaAsync(userId, 1, count));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InsufficientCurrency));
        Assert.That(exception.Message, Is.EqualTo("Insufficient premium currency"));
        // 차감 실패 시 캐릭터를 지급하지 않아야 한다
        _mockPlayerCharacterRepository.Verify(x => x.AddAsync(It.IsAny<PlayerCharacter>()), Times.Never);
    }

    [Test]
    public void PerformGachaAsync_AllCharactersOwned_ThrowsGameException()
    {
        // Arrange
        const long userId = 1;
        var user = CreateUser(userId, 1000, 0);
        var allCharacters = new Dictionary<int, CharacterMaster>
        {
            { 1, new CharacterMaster { Id = 1, Name = "Char1", Portfolio = 10, Development = 10, JobSearching = 10 } },
            { 2, new CharacterMaster { Id = 2, Name = "Char2", Portfolio = 20, Development = 20, JobSearching = 20 } }
        }.ToImmutableDictionary();
        List<PlayerCharacter> ownedCharacters =
        [
            CreatePlayerCharacter(userId, 1),
            CreatePlayerCharacter(userId, 2)
        ];

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockPlayerCharacterRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(ownedCharacters);
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters())
            .Returns(allCharacters);

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.PerformGachaAsync(userId, 1, 1));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidRequestHttpBody));
        Assert.That(exception.Message, Is.EqualTo("No new characters available to obtain"));
    }

    [Test]
    public async Task PerformGachaAsync_SuccessfulGacha_ReturnsCorrectResult()
    {
        // Arrange
        const long userId = 1;
        const int count = 2;
        const long expectedRemainingCurrency = 400; // 1000 - (2 * 300)

        var allCharacters = new Dictionary<int, CharacterMaster>
        {
            { 1, new CharacterMaster { Id = 1, Name = "Char1", Portfolio = 10, Development = 10, JobSearching = 10 } },
            { 2, new CharacterMaster { Id = 2, Name = "Char2", Portfolio = 20, Development = 20, JobSearching = 20 } },
            { 3, new CharacterMaster { Id = 3, Name = "Char3", Portfolio = 30, Development = 30, JobSearching = 30 } },
            { 4, new CharacterMaster { Id = 4, Name = "Char4", Portfolio = 40, Development = 40, JobSearching = 40 } }
        }.ToImmutableDictionary();
        List<PlayerCharacter> ownedCharacters = [CreatePlayerCharacter(userId, 1)];

        _mockPlayerCharacterRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(ownedCharacters);
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters())
            .Returns(allCharacters);
        _mockUserRepository.Setup(x => x.TryDeductPremiumCurrencyAsync(userId, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        // 차감은 체인지 트래커를 우회하므로, 응답 잔액은 커밋 후 재조회한 DB 상태에서 온다
        _mockUserRepository.Setup(x => x.GetByIdAsNoTrackingAsync(userId))
            .ReturnsAsync(CreateUser(userId, expectedRemainingCurrency, 500));

        // Act
        var result = await _service.PerformGachaAsync(userId, 1, count);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CharacterMasterIds, Has.Count.EqualTo(count));
        Assert.That(result.CharacterMasterIds, Does.Not.Contain(1)); // Should not contain owned character
        Assert.That(result.RemainingPremiumCurrency, Is.EqualTo(expectedRemainingCurrency));
        Assert.That(result.RemainingFreeCurrency, Is.EqualTo(500));

        // 정확한 비용(개수 × 300)으로 한 번만 차감을 시도해야 한다
        _mockUserRepository.Verify(x => x.TryDeductPremiumCurrencyAsync(userId, count * 300L, It.IsAny<CancellationToken>()), Times.Once);
        _mockPlayerCharacterRepository.Verify(x => x.AddAsync(It.IsAny<PlayerCharacter>()), Times.Exactly(count));

        // 쓰기 성공 후 조회 캐시가 무효화되어야 한다
        _mockPlayerCharacterCache.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task PerformGachaAsync_RequestMoreThanAvailable_ReturnsAvailableCount()
    {
        // Arrange
        const long userId = 1;
        const int requestedCount = 5; // Request more than available
        const long expectedRemainingCurrency = 1400; // 2000 - (2 * 300)

        var allCharacters = new Dictionary<int, CharacterMaster>
        {
            { 1, new CharacterMaster { Id = 1, Name = "Char1", Portfolio = 10, Development = 10, JobSearching = 10 } },
            { 2, new CharacterMaster { Id = 2, Name = "Char2", Portfolio = 20, Development = 20, JobSearching = 20 } }
        }.ToImmutableDictionary(); // Only 2 characters

        _mockPlayerCharacterRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<PlayerCharacter>()); // None owned
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters())
            .Returns(allCharacters);
        _mockUserRepository.Setup(x => x.TryDeductPremiumCurrencyAsync(userId, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserRepository.Setup(x => x.GetByIdAsNoTrackingAsync(userId))
            .ReturnsAsync(CreateUser(userId, expectedRemainingCurrency, 0));

        // Act
        var result = await _service.PerformGachaAsync(userId, 1, requestedCount);

        // Assert
        Assert.That(result.CharacterMasterIds, Has.Count.EqualTo(2)); // Only 2 available
        Assert.That(result.RemainingPremiumCurrency, Is.EqualTo(expectedRemainingCurrency));
        // 요청(5)이 아니라 실제 지급 개수(2)만큼만 차감해야 한다
        _mockUserRepository.Verify(x => x.TryDeductPremiumCurrencyAsync(userId, 2 * 300L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetPlayerCharactersAsync_ReturnsCorrectPlayerCharacterInfo()
    {
        // Arrange
        const long userId = 1;
        var playerCharacters = new List<PlayerCharacter>
        {
            CreatePlayerCharacter(userId, 1, 5, 100),
            CreatePlayerCharacter(userId, 2, 3, 50)
        };

        _mockPlayerCharacterRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(playerCharacters);

        // Act
        var result = await _service.GetPlayerCharactersAsync(userId);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));

        var first = result[0];
        Assert.That(first.CharacterMasterId, Is.EqualTo(1));
        Assert.That(first.Level, Is.EqualTo(5));
        Assert.That(first.CurrentExp, Is.EqualTo(100));

        var second = result[1];
        Assert.That(second.CharacterMasterId, Is.EqualTo(2));
        Assert.That(second.Level, Is.EqualTo(3));
        Assert.That(second.CurrentExp, Is.EqualTo(50));
    }

    private static User CreateUser(long id, long premiumCurrency, long freeCurrency)
    {
        return new User(
            accountId: id,
            nickname: "TestUser",
            level: 1,
            currentExp: 0,
            currentStamina: 100,
            maxStamina: 100,
            lastStaminaRecharge: DateTime.UtcNow,
            premiumCurrency: premiumCurrency,
            freeCurrency: freeCurrency,
            mainCharacterId: 1,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        ) { Id = id };
    }

    private static PlayerCharacter CreatePlayerCharacter(long userId, int characterMasterId, int level = 1, int currentExp = 0)
    {
        return new PlayerCharacter(
            userId: userId,
            characterMasterId: characterMasterId,
            level: level,
            currentExp: currentExp,
            obtainedAt: DateTime.UtcNow
        );
    }
}
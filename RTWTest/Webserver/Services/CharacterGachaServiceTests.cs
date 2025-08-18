using System.Collections.Immutable;
using Moq;
using NetworkDefinition.ErrorCode;
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
    private Mock<IGameUnitOfWork> _mockGameUnitOfWork;
    private Mock<IMasterDataProvider> _mockMasterDataProvider;
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IPlayerCharacterRepository> _mockPlayerCharacterRepository;
    private CharacterGachaService _service;

    [SetUp]
    public void SetUp()
    {
        _mockGameUnitOfWork = new Mock<IGameUnitOfWork>();
        _mockMasterDataProvider = new Mock<IMasterDataProvider>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPlayerCharacterRepository = new Mock<IPlayerCharacterRepository>();

        _mockGameUnitOfWork.Setup(x => x.UserRepository)
            .Returns(_mockUserRepository.Object);
        _mockGameUnitOfWork.Setup(x => x.PlayerCharacterRepository)
            .Returns(_mockPlayerCharacterRepository.Object);

        _service = new CharacterGachaService(_mockGameUnitOfWork.Object, _mockMasterDataProvider.Object);
    }

    [Test]
    public void PerformGachaAsync_UserNotFound_ThrowsGameException()
    {
        // Arrange
        const long userId = 1;
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(null as User);

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.PerformGachaAsync(userId, 1, 1));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.AccountNotFound));
        Assert.That(exception.Message, Is.EqualTo("User not found"));
    }

    [Test]
    public void PerformGachaAsync_InsufficientCurrency_ThrowsGameException()
    {
        // Arrange
        const long userId = 1;
        const int count = 2;
        const long initialCurrency = 500; // 600 needed for 2 gachas (300 each)

        var user = CreateUser(userId, initialCurrency, 0);
        var allCharacters = new Dictionary<int, CharacterMaster>
        {
            { 1, new CharacterMaster { Id = 1, Name = "Char1", Portfolio = 10, Development = 10, JobSearching = 10 } },
            { 2, new CharacterMaster { Id = 2, Name = "Char2", Portfolio = 20, Development = 20, JobSearching = 20 } },
        }.ToImmutableDictionary();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockPlayerCharacterRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<PlayerCharacter>());
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters())
            .Returns(allCharacters);

        // Act & Assert
        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.PerformGachaAsync(userId, 1, count));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InsufficientCurrency));
        Assert.That(exception.Message, Is.EqualTo("Insufficient premium currency"));
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
        const long initialCurrency = 1000;
        const long expectedRemainingCurrency = 400; // 1000 - (2 * 300)

        var user = CreateUser(userId, initialCurrency, 500);
        var allCharacters = new Dictionary<int, CharacterMaster>
        {
            { 1, new CharacterMaster { Id = 1, Name = "Char1", Portfolio = 10, Development = 10, JobSearching = 10 } },
            { 2, new CharacterMaster { Id = 2, Name = "Char2", Portfolio = 20, Development = 20, JobSearching = 20 } },
            { 3, new CharacterMaster { Id = 3, Name = "Char3", Portfolio = 30, Development = 30, JobSearching = 30 } },
            { 4, new CharacterMaster { Id = 4, Name = "Char4", Portfolio = 40, Development = 40, JobSearching = 40 } }
        }.ToImmutableDictionary();
        List<PlayerCharacter> ownedCharacters = [CreatePlayerCharacter(userId, 1)];

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockPlayerCharacterRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(ownedCharacters);
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters())
            .Returns(allCharacters);

        // Act
        var result = await _service.PerformGachaAsync(userId, 1, count);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CharacterMasterIds, Has.Count.EqualTo(count));
        Assert.That(result.CharacterMasterIds, Does.Not.Contain(1)); // Should not contain owned character
        Assert.That(result.RemainingPremiumCurrency, Is.EqualTo(expectedRemainingCurrency));
        Assert.That(result.RemainingFreeCurrency, Is.EqualTo(500));

        // Verify repository calls
        _mockPlayerCharacterRepository.Verify(x => x.AddAsync(It.IsAny<PlayerCharacter>()), Times.Exactly(count));
        _mockUserRepository.Verify(x => x.Update(user), Times.Once);
        _mockGameUnitOfWork.Verify(x => x.SaveAsync(), Times.Once);
    }

    [Test]
    public async Task PerformGachaAsync_RequestMoreThanAvailable_ReturnsAvailableCount()
    {
        // Arrange
        const long userId = 1;
        const int requestedCount = 5; // Request more than available
        const long initialCurrency = 2000;

        var user = CreateUser(userId, initialCurrency, 0);
        var allCharacters = new Dictionary<int, CharacterMaster>
        {
            { 1, new CharacterMaster { Id = 1, Name = "Char1", Portfolio = 10, Development = 10, JobSearching = 10 } },
            { 2, new CharacterMaster { Id = 2, Name = "Char2", Portfolio = 20, Development = 20, JobSearching = 20 } }
        }.ToImmutableDictionary(); // Only 2 characters

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockPlayerCharacterRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<PlayerCharacter>()); // None owned
        _mockMasterDataProvider.Setup(x => x.GetAllCharacters())
            .Returns(allCharacters);

        // Act
        var result = await _service.PerformGachaAsync(userId, 1, requestedCount);

        // Assert
        Assert.That(result.CharacterMasterIds, Has.Count.EqualTo(2)); // Only 2 available
        Assert.That(result.RemainingPremiumCurrency, Is.EqualTo(1400)); // 2000 - (2 * 300)
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
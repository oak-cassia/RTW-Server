using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetworkDefinition.ErrorCode;
using RTWWebServer.Data;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs.Request;
using RTWWebServer.Exceptions;
using RTWWebServer.MasterDatas.Models;
using RTWWebServer.Providers.MasterData;
using RTWWebServer.Services;

namespace RTWTest.Webserver.Services;

// 레이아웃 "교체"(삭제+삽입) 원자성 회귀 가드. EF InMemory는 트랜잭션을 지원하지 않으므로,
// 관계형 SQLite(in-memory)에 실제 리포지토리를 묶어 교체가 한 트랜잭션으로 적용되는지 검증한다.
[TestFixture]
public class LobbyServiceIntegrationTests
{
    private SqliteConnection _connection;
    private GameDbContext _dbContext;
    private PlayerLobbyFurnitureRepository _repository;
    private PlayerLobbyRepository _lobbyRepository;
    private UserRepository _userRepository;
    private Mock<IMasterDataProvider> _mockMasterDataProvider;
    private LobbyService _service;

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
        _repository = new PlayerLobbyFurnitureRepository(_dbContext);
        _lobbyRepository = new PlayerLobbyRepository(_dbContext);
        _userRepository = new UserRepository(_dbContext);
        _mockMasterDataProvider = new Mock<IMasterDataProvider>();
        SetupRoomGrades((1, 30, 30), (2, 50, 50), (3, 100, 100));
        // 증축의 랭크 게이트는 기본적으로 통과시킨다(랭크 자체 파생은 MasterDataProviderTests가 검증).
        _mockMasterDataProvider.Setup(p => p.GetRankByFame(It.IsAny<long>())).Returns(99);

        _service = new LobbyService(
            _dbContext,
            _repository,
            _lobbyRepository,
            _userRepository,
            _mockMasterDataProvider.Object,
            Mock.Of<ILogger<LobbyService>>());
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }

    [Test]
    public async Task SaveLobbyAsync_ReplacesExistingLayout()
    {
        const long userId = 7;
        SeedFurniture(userId, (2001, 0, 0), (2002, 1, 1)); // 기존 2개
        AllowAllFurniture();

        var newLayout = new[]
        {
            new LobbyFurniturePlacement(2003, 2, 2, 0),
            new LobbyFurniturePlacement(2004, 3, 3, 90),
            new LobbyFurniturePlacement(2004, 4, 4, 180) // 같은 가구 2개 허용
        };

        var result = await _service.SaveLobbyAsync(userId, newLayout);

        Assert.That(result.Furniture, Has.Length.EqualTo(3));

        await using var verify = CreateVerifyContext();
        var persisted = await verify.PlayerLobbyFurniture
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.PosX)
            .ToListAsync();
        Assert.That(persisted, Has.Count.EqualTo(3));
        // 기존 2001/2002는 사라지고 새 레이아웃만 남는다.
        Assert.That(persisted.Select(f => f.FurnitureMasterId), Is.EqualTo(new[] { 2003, 2004, 2004 }));
        Assert.That(persisted.All(f => f.Id > 0), Is.True);
    }

    [Test]
    public async Task SaveLobbyAsync_UnknownFurniture_LeavesStoredLayoutUnchanged()
    {
        const long userId = 7;
        SeedFurniture(userId, (2001, 0, 0)); // 기존 1개
        // 2003만 카탈로그 허용, 2099는 미허용
        _mockMasterDataProvider
            .Setup(p => p.TryGetFurniture(It.IsAny<int>(), out It.Ref<FurnitureMaster>.IsAny))
            .Returns((int id, out FurnitureMaster f) =>
            {
                f = new FurnitureMaster { Id = id, Name = $"F{id}", Category = 1, Width = 1, Height = 1 };
                return id == 2003;
            });

        var badLayout = new[]
        {
            new LobbyFurniturePlacement(2003, 1, 1, 0),
            new LobbyFurniturePlacement(2099, 2, 2, 0) // 미허용 → 거부
        };

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, badLayout));
        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));

        // 검증 실패는 트랜잭션 진입 전에 막으므로 기존 레이아웃이 그대로 남아야 한다.
        await using var verify = CreateVerifyContext();
        var persisted = await verify.PlayerLobbyFurniture.Where(f => f.UserId == userId).ToListAsync();
        Assert.That(persisted, Has.Count.EqualTo(1));
        Assert.That(persisted[0].FurnitureMasterId, Is.EqualTo(2001));
    }

    [Test]
    public async Task ExpandRoomAsync_PersistsAndIncrementsGrade()
    {
        const long userId = 7;
        SeedUser(userId, gold: 1_000_000, fame: 1000); // 기본 등급표는 비용 0이라 잔액은 충분하기만 하면 된다
        AllowAllFurniture();

        var first = await _service.ExpandRoomAsync(userId); // 행 없음 → 2등급 생성
        Assert.That(first.RoomGrade, Is.EqualTo(2));
        Assert.That(first.Width, Is.EqualTo(50));

        _dbContext.ChangeTracker.Clear(); // 요청 경계 모사

        var second = await _service.ExpandRoomAsync(userId); // 2 → 3등급
        Assert.That(second.RoomGrade, Is.EqualTo(3));
        Assert.That(second.Width, Is.EqualTo(100));

        await using var verify = CreateVerifyContext();
        var persisted = await verify.PlayerLobbies.SingleAsync(l => l.UserId == userId);
        Assert.That(persisted.RoomGrade, Is.EqualTo(3));
    }

    [Test]
    public async Task ExpandRoomAsync_DeductsGoldAtomically()
    {
        const long userId = 7;
        SeedUser(userId, gold: 5000, fame: 1000);
        SetupRoomGradeFull(2, 50, 50, requiredRank: 1, expandCost: 1000, CurrencyType.Free);
        _mockMasterDataProvider.Setup(p => p.GetRankByFame(1000)).Returns(2);

        var result = await _service.ExpandRoomAsync(userId);

        Assert.That(result.RoomGrade, Is.EqualTo(2));

        await using var verify = CreateVerifyContext();
        var user = await verify.Users.SingleAsync(u => u.Id == userId);
        Assert.That(user.FreeCurrency, Is.EqualTo(4000)); // 5000 - 1000
        var lobby = await verify.PlayerLobbies.SingleAsync(l => l.UserId == userId);
        Assert.That(lobby.RoomGrade, Is.EqualTo(2));
    }

    [Test]
    public void ExpandRoomAsync_InsufficientGold_LeavesGradeAndGoldUnchanged()
    {
        const long userId = 7;
        SeedUser(userId, gold: 500, fame: 1000); // 비용 1000 > 보유 500 → 차감 실패
        SetupRoomGradeFull(2, 50, 50, requiredRank: 1, expandCost: 1000, CurrencyType.Free);
        _mockMasterDataProvider.Setup(p => p.GetRankByFame(1000)).Returns(2);

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.ExpandRoomAsync(userId));
        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InsufficientCurrency));

        // 트랜잭션 롤백으로 골드도 등급도 그대로여야 한다(부분 적용 없음).
        using var verify = CreateVerifyContext();
        var user = verify.Users.Single(u => u.Id == userId);
        Assert.That(user.FreeCurrency, Is.EqualTo(500));
        Assert.That(verify.PlayerLobbies.Any(l => l.UserId == userId), Is.False);
    }

    [Test]
    public async Task SaveLobbyAsync_BoundsFollowRoomGrade()
    {
        const long userId = 7;
        SeedUser(userId, gold: 1_000_000, fame: 1000);
        AllowAllFurniture();
        var nearEdge = new[] { new LobbyFurniturePlacement(2001, 40, 0, 0) };

        // 기본 1등급(30x30): PosX=40은 경계 밖 → 거부
        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, nearEdge));
        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));

        // 2등급(50x50)로 확장하면 같은 좌표가 경계 안 → 허용
        await _service.ExpandRoomAsync(userId);
        _dbContext.ChangeTracker.Clear();

        var result = await _service.SaveLobbyAsync(userId, nearEdge);
        Assert.That(result.RoomGrade, Is.EqualTo(2));
        Assert.That(result.Furniture, Has.Length.EqualTo(1));
    }

    private void SeedFurniture(long userId, params (int masterId, int x, int y)[] rows)
    {
        foreach (var (masterId, x, y) in rows)
        {
            _dbContext.PlayerLobbyFurniture.Add(new PlayerLobbyFurniture(userId, masterId, x, y, 0));
        }

        _dbContext.SaveChanges();
        // 시드로 추적된 엔티티가 이후 서비스 흐름에 끼어들지 않도록 트래커를 비운다(요청 경계 모사).
        _dbContext.ChangeTracker.Clear();
    }

    private void SeedUser(long userId, long gold, long fame)
    {
        var user = new User(accountId: userId, nickname: $"user{userId}", level: 1, currentExp: 0,
            currentStamina: 10, maxStamina: 10, lastStaminaRecharge: DateTime.UtcNow,
            premiumCurrency: 0, freeCurrency: gold, mainCharacterId: 1,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow) { Id = userId, Fame = fame };
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear(); // 요청 경계 모사
    }

    // 단일 등급에 비용/랭크 게이트까지 포함한 마스터를 깐다. SetupRoomGrades의 일반 설정을 해당 등급에서만 덮어쓴다.
    private void SetupRoomGradeFull(int grade, int width, int height, int requiredRank, long expandCost,
        CurrencyType currency)
    {
        _mockMasterDataProvider
            .Setup(p => p.TryGetRoomGrade(grade, out It.Ref<RoomGradeMaster>.IsAny))
            .Returns((int g, out RoomGradeMaster rg) =>
            {
                rg = new RoomGradeMaster
                {
                    Grade = grade, Width = width, Height = height,
                    RequiredRank = requiredRank, ExpandCost = expandCost, ExpandCurrency = currency
                };
                return true;
            });
    }

    private void AllowAllFurniture()
    {
        _mockMasterDataProvider
            .Setup(p => p.TryGetFurniture(It.IsAny<int>(), out It.Ref<FurnitureMaster>.IsAny))
            .Returns((int id, out FurnitureMaster f) =>
            {
                f = new FurnitureMaster { Id = id, Name = $"F{id}", Category = 1, Width = 1, Height = 1 };
                return true;
            });
    }

    private void SetupRoomGrades(params (int grade, int width, int height)[] grades)
    {
        var map = grades.ToDictionary(g => g.grade, g => (g.width, g.height));
        _mockMasterDataProvider
            .Setup(p => p.TryGetRoomGrade(It.IsAny<int>(), out It.Ref<RoomGradeMaster>.IsAny))
            .Returns((int grade, out RoomGradeMaster rg) =>
            {
                if (map.TryGetValue(grade, out var size))
                {
                    rg = new RoomGradeMaster { Grade = grade, Width = size.width, Height = size.height };
                    return true;
                }

                rg = null!;
                return false;
            });
    }

    private GameDbContext CreateVerifyContext()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new GameDbContext(options);
    }
}

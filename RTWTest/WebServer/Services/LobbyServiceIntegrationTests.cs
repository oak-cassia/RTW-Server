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
        _mockMasterDataProvider = new Mock<IMasterDataProvider>();

        _service = new LobbyService(
            _dbContext,
            _repository,
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

    private GameDbContext CreateVerifyContext()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new GameDbContext(options);
    }
}

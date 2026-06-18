using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

[TestFixture]
public class LobbyServiceTests
{
    private GameDbContext _dbContext;
    private Mock<IPlayerLobbyFurnitureRepository> _mockRepository;
    private Mock<IPlayerLobbyRepository> _mockLobbyRepository;
    private Mock<IMasterDataProvider> _mockMasterDataProvider;
    private LobbyService _service;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            // InMemory는 트랜잭션을 지원하지 않으므로 서비스의 BeginTransaction을 무시(no-op) 처리한다.
            // 삭제+삽입 교체의 실제 원자성은 LobbyServiceIntegrationTests(SQLite)에서 검증한다.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new GameDbContext(options);

        _mockRepository = new Mock<IPlayerLobbyFurnitureRepository>();
        _mockLobbyRepository = new Mock<IPlayerLobbyRepository>();
        _mockMasterDataProvider = new Mock<IMasterDataProvider>();

        // 방 크기 계산은 항상 마스터를 거치므로 기본 등급표를 깔아 둔다.
        // (PlayerLobby 행이 없으면 1등급으로 간주된다.)
        SetupRoomGrades((1, 30, 30), (2, 50, 50), (3, 100, 100));

        _service = new LobbyService(
            _dbContext,
            _mockRepository.Object,
            _mockLobbyRepository.Object,
            _mockMasterDataProvider.Object,
            Mock.Of<ILogger<LobbyService>>());
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
    }

    [Test]
    public async Task SaveLobbyAsync_ValidItems_ReplacesLayout()
    {
        const long userId = 42;
        AllowFurniture(2001, 2002);
        var items = new[]
        {
            new LobbyFurniturePlacement(2001, 1, 2, 0),
            new LobbyFurniturePlacement(2002, 3, 4, 90)
        };
        // 응답은 커밋 후 재조회 결과를 반영한다.
        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new[]
            {
                new PlayerLobbyFurniture(userId, 2001, 1, 2, 0) { Id = 1 },
                new PlayerLobbyFurniture(userId, 2002, 3, 4, 90) { Id = 2 }
            });

        var result = await _service.SaveLobbyAsync(userId, items);

        _mockRepository.Verify(r => r.RemoveByUserIdAsync(userId), Times.Once);
        _mockRepository.Verify(
            r => r.AddRangeAsync(It.Is<IEnumerable<PlayerLobbyFurniture>>(e => e.Count() == 2)),
            Times.Once);
        Assert.That(result.Furniture, Has.Length.EqualTo(2));
        Assert.That(result.Furniture[0].FurnitureMasterId, Is.EqualTo(2001));
        Assert.That(result.Furniture[1].Rotation, Is.EqualTo(90));
    }

    [Test]
    public void SaveLobbyAsync_UnknownFurniture_ThrowsAndDoesNotPersist()
    {
        const long userId = 42;
        AllowFurniture(2001); // 2002는 카탈로그에 없음
        var items = new[]
        {
            new LobbyFurniturePlacement(2001, 0, 0, 0),
            new LobbyFurniturePlacement(2002, 0, 0, 0)
        };

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, items));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));
        // 검증 실패는 트랜잭션 진입 전에 막으므로 어떤 쓰기 작업도 일어나지 않는다.
        _mockRepository.Verify(r => r.RemoveByUserIdAsync(It.IsAny<long>()), Times.Never);
        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<PlayerLobbyFurniture>>()), Times.Never);
    }

    [Test]
    public void SaveLobbyAsync_ExceedsMaxItems_Throws()
    {
        const long userId = 42;
        AllowFurniture(2001);
        var items = Enumerable.Range(0, 201)
            .Select(_ => new LobbyFurniturePlacement(2001, 0, 0, 0))
            .ToArray();

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, items));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));
        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<PlayerLobbyFurniture>>()), Times.Never);
    }

    [Test]
    public async Task SaveLobbyAsync_DuplicateFurnitureIds_Allowed()
    {
        const long userId = 42;
        AllowFurniture(2002);
        // 같은 의자 두 개를 배치하는 것은 정상이다.
        var items = new[]
        {
            new LobbyFurniturePlacement(2002, 1, 1, 0),
            new LobbyFurniturePlacement(2002, 5, 5, 0)
        };
        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(Array.Empty<PlayerLobbyFurniture>());

        await _service.SaveLobbyAsync(userId, items);

        _mockRepository.Verify(
            r => r.AddRangeAsync(It.Is<IEnumerable<PlayerLobbyFurniture>>(e => e.Count() == 2)),
            Times.Once);
    }

    [Test]
    public async Task SaveLobbyAsync_EmptyLayout_ClearsRoom()
    {
        const long userId = 42;
        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(Array.Empty<PlayerLobbyFurniture>());

        var result = await _service.SaveLobbyAsync(userId, Array.Empty<LobbyFurniturePlacement>());

        _mockRepository.Verify(r => r.RemoveByUserIdAsync(userId), Times.Once);
        _mockRepository.Verify(
            r => r.AddRangeAsync(It.Is<IEnumerable<PlayerLobbyFurniture>>(e => !e.Any())),
            Times.Once);
        Assert.That(result.Furniture, Is.Empty);
    }

    [Test]
    public async Task GetLobbyAsync_MapsRows()
    {
        const long userId = 42;
        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new[]
            {
                new PlayerLobbyFurniture(userId, 2003, 7, 8, 180) { Id = 5 }
            });

        var result = await _service.GetLobbyAsync(userId);

        Assert.That(result.Furniture, Has.Length.EqualTo(1));
        var info = result.Furniture[0];
        Assert.That(info.Id, Is.EqualTo(5));
        Assert.That(info.FurnitureMasterId, Is.EqualTo(2003));
        Assert.That(info.PosX, Is.EqualTo(7));
        Assert.That(info.PosY, Is.EqualTo(8));
        Assert.That(info.Rotation, Is.EqualTo(180));
    }

    [Test]
    public void SaveLobbyAsync_OutOfBounds_ThrowsAndDoesNotPersist()
    {
        const long userId = 42;
        AllowFurniture(2001); // 기본 1등급 = 30x30, 유효 좌표 0..29
        var items = new[]
        {
            new LobbyFurniturePlacement(2001, 30, 0, 0) // PosX=30 → 경계 밖
        };

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, items));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));
        _mockRepository.Verify(r => r.RemoveByUserIdAsync(It.IsAny<long>()), Times.Never);
        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<PlayerLobbyFurniture>>()), Times.Never);
    }

    [Test]
    public void SaveLobbyAsync_NegativeCoordinate_Throws()
    {
        const long userId = 42;
        AllowFurniture(2001);
        var items = new[]
        {
            new LobbyFurniturePlacement(2001, 0, -1, 0) // 음수 좌표도 거부
        };

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, items));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));
    }

    [Test]
    public async Task GetLobbyAsync_NoRow_ReturnsDefaultGradeAndSize()
    {
        const long userId = 42;

        var result = await _service.GetLobbyAsync(userId);

        Assert.That(result.RoomGrade, Is.EqualTo(1));
        Assert.That(result.Width, Is.EqualTo(30));
        Assert.That(result.Height, Is.EqualTo(30));
    }

    [Test]
    public async Task GetLobbyAsync_ExistingGrade_ReturnsMatchingSize()
    {
        const long userId = 42;
        _mockLobbyRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new PlayerLobby(userId, 2) { Id = 1 });

        var result = await _service.GetLobbyAsync(userId);

        Assert.That(result.RoomGrade, Is.EqualTo(2));
        Assert.That(result.Width, Is.EqualTo(50));
        Assert.That(result.Height, Is.EqualTo(50));
    }

    [Test]
    public async Task ExpandRoomAsync_NoRow_CreatesGradeTwo()
    {
        const long userId = 42;

        var result = await _service.ExpandRoomAsync(userId);

        _mockLobbyRepository.Verify(
            r => r.AddAsync(It.Is<PlayerLobby>(l => l.UserId == userId && l.RoomGrade == 2)),
            Times.Once);
        _mockLobbyRepository.Verify(r => r.Update(It.IsAny<PlayerLobby>()), Times.Never);
        Assert.That(result.RoomGrade, Is.EqualTo(2));
        Assert.That(result.Width, Is.EqualTo(50));
    }

    [Test]
    public async Task ExpandRoomAsync_ExistingGrade_IncrementsAndUpdates()
    {
        const long userId = 42;
        _mockLobbyRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new PlayerLobby(userId, 2) { Id = 1 });

        var result = await _service.ExpandRoomAsync(userId);

        _mockLobbyRepository.Verify(
            r => r.Update(It.Is<PlayerLobby>(l => l.RoomGrade == 3)),
            Times.Once);
        _mockLobbyRepository.Verify(r => r.AddAsync(It.IsAny<PlayerLobby>()), Times.Never);
        Assert.That(result.RoomGrade, Is.EqualTo(3));
        Assert.That(result.Width, Is.EqualTo(100));
    }

    [Test]
    public void ExpandRoomAsync_AtMaxGrade_Throws()
    {
        const long userId = 42;
        _mockLobbyRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new PlayerLobby(userId, 3) { Id = 1 }); // 3등급이 최대(마스터에 4 없음)

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.ExpandRoomAsync(userId));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));
        _mockLobbyRepository.Verify(r => r.AddAsync(It.IsAny<PlayerLobby>()), Times.Never);
        _mockLobbyRepository.Verify(r => r.Update(It.IsAny<PlayerLobby>()), Times.Never);
    }

    [Test]
    public void SaveLobbyAsync_FootprintExceedsBounds_Throws()
    {
        const long userId = 42;
        AllowFurnitureSized((3001, 3, 2)); // 3x2 가구
        // 앵커(28,0)는 방(30x30) 안이지만 너비 3이 31까지 뻗어 경계를 넘는다(앵커-only 검사로는 통과했을 케이스).
        var items = new[] { new LobbyFurniturePlacement(3001, 28, 0, 0) };

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, items));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));
        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<PlayerLobbyFurniture>>()), Times.Never);
    }

    [Test]
    public async Task SaveLobbyAsync_Rotation90_SwapsFootprintAndFits()
    {
        const long userId = 42;
        AllowFurnitureSized((3001, 3, 1)); // 3x1 가구
        // 앵커(29,0)에서 회전 0이면 너비 3이 경계를 넘지만, 90도 회전 시 1x3이 되어 들어맞는다.
        var items = new[] { new LobbyFurniturePlacement(3001, 29, 0, 90) };
        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new[] { new PlayerLobbyFurniture(userId, 3001, 29, 0, 90) { Id = 1 } });

        var result = await _service.SaveLobbyAsync(userId, items);

        Assert.That(result.Furniture, Has.Length.EqualTo(1));
        _mockRepository.Verify(
            r => r.AddRangeAsync(It.Is<IEnumerable<PlayerLobbyFurniture>>(e => e.Count() == 1)),
            Times.Once);
    }

    [Test]
    public void SaveLobbyAsync_Rotation0_FootprintExceeds_Throws()
    {
        const long userId = 42;
        AllowFurnitureSized((3001, 3, 1)); // 동일 가구, 회전만 0
        var items = new[] { new LobbyFurniturePlacement(3001, 29, 0, 0) }; // 너비 3 → 경계 밖

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, items));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));
    }

    [Test]
    public void SaveLobbyAsync_OverlappingFurniture_Throws()
    {
        const long userId = 42;
        AllowFurnitureSized((3001, 2, 2));
        var items = new[]
        {
            new LobbyFurniturePlacement(3001, 0, 0, 0), // (0,0)~(1,1)
            new LobbyFurniturePlacement(3001, 1, 1, 0)  // (1,1)~(2,2) → (1,1) 칸이 겹침
        };

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, items));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));
        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<PlayerLobbyFurniture>>()), Times.Never);
    }

    [Test]
    public async Task SaveLobbyAsync_AdjacentFurniture_Allowed()
    {
        const long userId = 42;
        AllowFurnitureSized((3001, 2, 2));
        var items = new[]
        {
            new LobbyFurniturePlacement(3001, 0, 0, 0),
            new LobbyFurniturePlacement(3001, 2, 0, 0) // 맞닿지만 겹치지 않음
        };
        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(Array.Empty<PlayerLobbyFurniture>());

        await _service.SaveLobbyAsync(userId, items);

        _mockRepository.Verify(
            r => r.AddRangeAsync(It.Is<IEnumerable<PlayerLobbyFurniture>>(e => e.Count() == 2)),
            Times.Once);
    }

    [Test]
    public void SaveLobbyAsync_NonRightAngleRotation_Throws()
    {
        const long userId = 42;
        AllowFurnitureSized((3001, 1, 1));
        var items = new[] { new LobbyFurniturePlacement(3001, 0, 0, 45) }; // 90도 단위 아님

        var exception = Assert.ThrowsAsync<GameException>(async () =>
            await _service.SaveLobbyAsync(userId, items));

        Assert.That(exception.ErrorCode, Is.EqualTo(WebServerErrorCode.InvalidArgument));
    }

    private void AllowFurnitureSized(params (int id, int width, int height)[] furniture)
    {
        var map = furniture.ToDictionary(f => f.id, f => (f.width, f.height));
        _mockMasterDataProvider
            .Setup(p => p.TryGetFurniture(It.IsAny<int>(), out It.Ref<FurnitureMaster>.IsAny))
            .Returns((int id, out FurnitureMaster f) =>
            {
                if (map.TryGetValue(id, out var size))
                {
                    f = new FurnitureMaster { Id = id, Name = $"F{id}", Category = 1, Width = size.width, Height = size.height };
                    return true;
                }

                f = null!;
                return false;
            });
    }

    private void AllowFurniture(params int[] ids)
    {
        var set = ids.ToHashSet();
        _mockMasterDataProvider
            .Setup(p => p.TryGetFurniture(It.IsAny<int>(), out It.Ref<FurnitureMaster>.IsAny))
            .Returns((int id, out FurnitureMaster f) =>
            {
                f = new FurnitureMaster { Id = id, Name = $"F{id}", Category = 1, Width = 1, Height = 1 };
                return set.Contains(id);
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
}

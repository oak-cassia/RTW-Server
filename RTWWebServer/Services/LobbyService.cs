using RTWWebServer.Data;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.DTOs.Request;
using RTWWebServer.Exceptions;
using RTWWebServer.Providers.MasterData;
using NetworkDefinition.ErrorCode;

namespace RTWWebServer.Services;

public class LobbyService(
    GameDbContext dbContext,
    IPlayerLobbyFurnitureRepository lobbyFurnitureRepository,
    IPlayerLobbyRepository lobbyRepository,
    IMasterDataProvider masterDataProvider,
    ILogger<LobbyService> logger
) : ILobbyService
{
    // 한 방에 둘 수 있는 가구 수 상한. 악의적/오작동 클라이언트가 거대한 레이아웃을 보내는 것을 막는다.
    const int MAX_FURNITURE_PER_LOBBY = 200;

    // 방 행이 없는 유저의 기본 등급. 회원가입 흐름을 건드리지 않고 확장 시점에만 행을 만든다.
    const int DEFAULT_ROOM_GRADE = 1;

    public async Task<LobbyInfo> GetLobbyAsync(long userId)
    {
        var (grade, width, height) = await ResolveRoomAsync(userId);
        var furniture = await lobbyFurnitureRepository.GetByUserIdAsync(userId);
        return ToLobbyInfo(grade, width, height, furniture);
    }

    public async Task<LobbyInfo> SaveLobbyAsync(long userId, IReadOnlyList<LobbyFurniturePlacement> items)
    {
        if (items.Count > MAX_FURNITURE_PER_LOBBY)
        {
            throw new GameException(
                $"Too many furniture items: {items.Count} (max {MAX_FURNITURE_PER_LOBBY})",
                WebServerErrorCode.InvalidArgument);
        }

        var (grade, width, height) = await ResolveRoomAsync(userId);

        foreach (var item in items)
        {
            // 마스터 카탈로그에 없는 가구는 배치할 수 없다. 소유 개념은 아직 없으므로 카탈로그 존재 여부만 검사한다.
            if (!masterDataProvider.TryGetFurniture(item.FurnitureMasterId, out _))
            {
                throw new GameException(
                    $"Unknown furniture master id: {item.FurnitureMasterId}",
                    WebServerErrorCode.InvalidArgument);
            }

            // 배치 좌표가 방 경계 안(앵커 기준)인지 검증한다. 가구 footprint·겹침 검사는 별도 단계로 분리한다.
            if (item.PosX < 0 || item.PosX >= width || item.PosY < 0 || item.PosY >= height)
            {
                throw new GameException(
                    $"Furniture placement out of room bounds: ({item.PosX},{item.PosY}) not in [0,{width})x[0,{height})",
                    WebServerErrorCode.InvalidArgument);
            }
        }

        // 레이아웃 저장은 "방 전체 교체"다. 기존 행 삭제와 새 행 삽입을 한 트랜잭션으로 묶어,
        // 도중 실패 시 이전 레이아웃이 그대로 남도록(부분 저장 방지) 한다.
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            await lobbyFurnitureRepository.RemoveByUserIdAsync(userId);

            var entities = items.Select(item => new PlayerLobbyFurniture(
                userId: userId,
                furnitureMasterId: item.FurnitureMasterId,
                posX: item.PosX,
                posY: item.PosY,
                rotation: item.Rotation));

            await lobbyFurnitureRepository.AddRangeAsync(entities);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is not GameException)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to save lobby layout for userId: {UserId}", userId);
            throw;
        }

        // 삽입된 행의 생성 Id/UpdatedAt를 반영해 응답하려고 커밋 후 다시 읽는다.
        var saved = await lobbyFurnitureRepository.GetByUserIdAsync(userId);
        return ToLobbyInfo(grade, width, height, saved);
    }

    public async Task<LobbyInfo> ExpandRoomAsync(long userId)
    {
        var lobby = await lobbyRepository.GetByUserIdAsync(userId);
        int currentGrade = lobby?.RoomGrade ?? DEFAULT_ROOM_GRADE;
        int nextGrade = currentGrade + 1;

        // 다음 등급이 마스터에 없으면 이미 최대 등급이다.
        if (!masterDataProvider.TryGetRoomGrade(nextGrade, out _))
        {
            throw new GameException(
                $"Room is already at max grade: {currentGrade}",
                WebServerErrorCode.InvalidArgument);
        }

        // 확장 비용/게이팅(예: 공헌치)은 아직 없다. 추후 여기에서 RoomGradeMaster의 요구치를 검사·차감한다.
        // 동시 확장은 RequestLockingMiddleware가 유저별로 직렬화하고, uk_lobby_user_id가 중복 행을 막는다.
        if (lobby is null)
        {
            await lobbyRepository.AddAsync(new PlayerLobby(userId, nextGrade));
        }
        else
        {
            lobby.RoomGrade = nextGrade;
            lobby.UpdatedAt = DateTime.UtcNow;
            lobbyRepository.Update(lobby);
        }

        await dbContext.SaveChangesAsync();

        var (width, height) = ResolveRoomSize(nextGrade);
        var furniture = await lobbyFurnitureRepository.GetByUserIdAsync(userId);
        return ToLobbyInfo(nextGrade, width, height, furniture);
    }

    private async Task<(int grade, int width, int height)> ResolveRoomAsync(long userId)
    {
        var lobby = await lobbyRepository.GetByUserIdAsync(userId);
        int grade = lobby?.RoomGrade ?? DEFAULT_ROOM_GRADE;
        var (width, height) = ResolveRoomSize(grade);
        return (grade, width, height);
    }

    private (int width, int height) ResolveRoomSize(int grade)
    {
        if (!masterDataProvider.TryGetRoomGrade(grade, out var roomGrade))
        {
            // 마스터에 없는 등급 = 서버 설정 오류(검증기가 1등급 존재를 보장하고, 확장도 마스터를 거친다).
            throw new InvalidOperationException($"Room grade master not found: {grade}");
        }

        return (roomGrade.Width, roomGrade.Height);
    }

    private static LobbyInfo ToLobbyInfo(int grade, int width, int height, IEnumerable<PlayerLobbyFurniture> furniture)
    {
        return new LobbyInfo
        {
            RoomGrade = grade,
            Width = width,
            Height = height,
            Furniture = furniture.Select(f => new LobbyFurnitureInfo
            {
                Id = f.Id,
                FurnitureMasterId = f.FurnitureMasterId,
                PosX = f.PosX,
                PosY = f.PosY,
                Rotation = f.Rotation,
                UpdatedAt = f.UpdatedAt
            }).ToArray()
        };
    }
}

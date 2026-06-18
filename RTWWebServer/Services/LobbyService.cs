using RTWWebServer.Data;
using RTWWebServer.Data.Entities;
using RTWWebServer.Data.Repositories;
using RTWWebServer.DTOs;
using RTWWebServer.DTOs.Request;
using RTWWebServer.Exceptions;
using RTWWebServer.MasterDatas.Models;
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

        ValidatePlacements(items, width, height);

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

    // 레이아웃 무결성 검증. 서버가 레이아웃의 권위 저장소이므로, 물리적으로 불가능한 배치
    // (미등록 가구·90도 외 회전·방 경계 초과·가구 간 겹침)가 저장되지 않도록 여기서 모두 차단한다.
    private void ValidatePlacements(IReadOnlyList<LobbyFurniturePlacement> items, int width, int height)
    {
        // 점유한 칸을 추적해 가구 간 겹침을 검출한다. 항상 0 <= y < height 이므로 (x * height + y)는 칸의 유일 키가 된다.
        var occupiedCells = new HashSet<long>();

        foreach (var item in items)
        {
            // 마스터 카탈로그에 없는 가구는 배치할 수 없다. 소유 개념은 아직 없으므로 카탈로그 존재 여부만 검사한다.
            if (!masterDataProvider.TryGetFurniture(item.FurnitureMasterId, out var furniture))
            {
                throw new GameException(
                    $"Unknown furniture master id: {item.FurnitureMasterId}",
                    WebServerErrorCode.InvalidArgument);
            }

            // 회전은 90도 단위만 허용한다(그리드 점유는 90도 배수에서만 정의된다). DTO에서도 막지만 서비스가 최종 권위를 가진다.
            if (item.Rotation is not (LobbyRotation.Degrees0 or LobbyRotation.Degrees90 or LobbyRotation.Degrees180 or LobbyRotation.Degrees270))
            {
                throw new GameException(
                    $"Invalid rotation: {item.Rotation} (must be 0, 90, 180, or 270)",
                    WebServerErrorCode.InvalidArgument);
            }

            var (footprintWidth, footprintHeight) = GetFootprint(furniture, item.Rotation);

            // 가구의 점유 영역(footprint) 전체가 방 안에 들어와야 한다. 앵커(PosX,PosY)는 영역의 원점 칸이다.
            if (item.PosX < 0 || item.PosY < 0 ||
                item.PosX + footprintWidth > width || item.PosY + footprintHeight > height)
            {
                throw new GameException(
                    $"Furniture placement out of room bounds: ({item.PosX},{item.PosY}) size {footprintWidth}x{footprintHeight} not in [0,{width})x[0,{height})",
                    WebServerErrorCode.InvalidArgument);
            }

            // 점유 칸이 다른 가구와 겹치면 거부한다(레이어/스택 개념은 아직 없다).
            for (int dx = 0; dx < footprintWidth; dx++)
            {
                for (int dy = 0; dy < footprintHeight; dy++)
                {
                    long cell = (long)(item.PosX + dx) * height + (item.PosY + dy);
                    if (!occupiedCells.Add(cell))
                    {
                        throw new GameException(
                            $"Furniture overlaps another item at ({item.PosX + dx},{item.PosY + dy})",
                            WebServerErrorCode.InvalidArgument);
                    }
                }
            }
        }
    }

    // 가구의 실제 점유 크기. 90/270도 회전 시 가로·세로가 뒤바뀐다.
    private static (int width, int height) GetFootprint(FurnitureMaster furniture, int rotation)
    {
        return rotation is LobbyRotation.Degrees90 or LobbyRotation.Degrees270
            ? (furniture.Height, furniture.Width)
            : (furniture.Width, furniture.Height);
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

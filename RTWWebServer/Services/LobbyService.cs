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
    IMasterDataProvider masterDataProvider,
    ILogger<LobbyService> logger
) : ILobbyService
{
    // 한 방에 둘 수 있는 가구 수 상한. 악의적/오작동 클라이언트가 거대한 레이아웃을 보내는 것을 막는다.
    const int MAX_FURNITURE_PER_LOBBY = 200;

    public async Task<LobbyInfo> GetLobbyAsync(long userId)
    {
        var furniture = await lobbyFurnitureRepository.GetByUserIdAsync(userId);
        return ToLobbyInfo(furniture);
    }

    public async Task<LobbyInfo> SaveLobbyAsync(long userId, IReadOnlyList<LobbyFurniturePlacement> items)
    {
        if (items.Count > MAX_FURNITURE_PER_LOBBY)
        {
            throw new GameException(
                $"Too many furniture items: {items.Count} (max {MAX_FURNITURE_PER_LOBBY})",
                WebServerErrorCode.InvalidArgument);
        }

        // 마스터 카탈로그에 없는 가구는 배치할 수 없다. 소유 개념은 v1에 없으므로 카탈로그 존재 여부만 검사한다.
        foreach (var item in items)
        {
            if (!masterDataProvider.TryGetFurniture(item.FurnitureMasterId, out _))
            {
                throw new GameException(
                    $"Unknown furniture master id: {item.FurnitureMasterId}",
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
        return ToLobbyInfo(saved);
    }

    private static LobbyInfo ToLobbyInfo(IEnumerable<PlayerLobbyFurniture> furniture)
    {
        return new LobbyInfo
        {
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

using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.DTOs.Request;

public record SaveLobbyRequest(
    [Required]
    LobbyFurniturePlacement[] Items);

public record LobbyFurniturePlacement(
    [Range(1, int.MaxValue, ErrorMessage = "FurnitureMasterId must be greater than 0")]
    int FurnitureMasterId,
    int PosX,
    int PosY,
    [Range(0, 359, ErrorMessage = "Rotation must be between 0 and 359")]
    int Rotation);

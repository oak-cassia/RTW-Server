using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.DTOs.Request;

public record CharacterGachaRequest(
    int GachaType,
    [Range(1, int.MaxValue, ErrorMessage = "Count must be greater than 0")]
    int Count);
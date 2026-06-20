using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.DTOs.Request;

public record StartMissionRequest(
    [Range(1, int.MaxValue, ErrorMessage = "MissionId must be greater than 0")]
    int MissionId);

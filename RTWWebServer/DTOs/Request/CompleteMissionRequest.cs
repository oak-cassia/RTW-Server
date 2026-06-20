using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.DTOs.Request;

public record CompleteMissionRequest(
    [Required(ErrorMessage = "TicketId is required")]
    string TicketId);

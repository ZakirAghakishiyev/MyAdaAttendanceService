namespace MyAdaAttendanceService.Application.DTOs;

public class QrScanResponseDto
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public int SessionId { get; set; }
    public int? ActivationId { get; set; }
    public int ValidScanCount { get; set; }

    public string? Status { get; set; }
    public DateTime? ScannedAt { get; set; }
}

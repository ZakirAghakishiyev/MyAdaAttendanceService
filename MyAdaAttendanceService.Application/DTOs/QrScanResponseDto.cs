namespace MyAdaAttendanceService.Application.DTOs;

public class QrScanResponseDto
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public int SessionId { get; set; }
    public int? ActivationId { get; set; }

    /// <summary>1 or 2 for a successful scan; which QR round this scan was for.</summary>
    public byte? Round { get; set; }

    /// <summary>How many distinct rounds (1 or 2) the student has successfully scanned in this session.</summary>
    public int ValidScanCount { get; set; }

    public string? Status { get; set; }
    public DateTime? ScannedAt { get; set; }
}

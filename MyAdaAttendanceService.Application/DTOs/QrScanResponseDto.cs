using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Application.DTOs;

public class QrScanResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public int SessionId { get; set; }
    public AttendanceStatus? Status { get; set; }

    public DateTime? ScannedAt { get; set; }
}

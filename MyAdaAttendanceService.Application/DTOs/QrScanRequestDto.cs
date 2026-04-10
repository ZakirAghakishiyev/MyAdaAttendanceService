namespace MyAdaAttendanceService.Application.DTOs;

public class QrScanRequestDto
{
    public string Token { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
}

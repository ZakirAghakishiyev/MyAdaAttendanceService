namespace MyAdaAttendanceService.Application.DTOs;

public class QrScanRequestDto
{
    public Guid StudentId { get; set; }
    public string Token { get; set; } = string.Empty;
    public QrContextDto? QrContext { get; set; }
    public string? DeviceInfo { get; set; }
}

public class QrContextDto
{
    public int? SessionId { get; set; }
    public int? RoundCount { get; set; }
    public string? InstructorJwt { get; set; }
}

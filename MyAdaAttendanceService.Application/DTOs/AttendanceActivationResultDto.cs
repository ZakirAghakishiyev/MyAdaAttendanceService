namespace MyAdaAttendanceService.Application.DTOs;

public class AttendanceActivationResultDto
{
    public int SessionId { get; set; }
    public bool IsAttendanceActive { get; set; }

    public DateTime? AttendanceActivatedAt { get; set; }
    public DateTime? AttendanceDeactivatedAt { get; set; }

    public string Message { get; set; } = string.Empty;
}

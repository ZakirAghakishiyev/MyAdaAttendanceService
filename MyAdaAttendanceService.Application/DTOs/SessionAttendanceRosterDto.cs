namespace MyAdaAttendanceService.Application.DTOs;

public class SessionAttendanceRosterDto
{
    public int SessionId { get; set; }
    public string LessonName { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public bool IsAttendanceActive { get; set; }
    public DateTime? AttendanceActivatedAt { get; set; }
    public DateTime? AttendanceDeactivatedAt { get; set; }
    public AttendanceSummaryDto Summary { get; set; } = new();
    public ICollection<AttendanceDto> Students { get; set; } = new List<AttendanceDto>();
}
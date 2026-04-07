namespace MyAdaAttendanceService.Application.DTOs;

public class SessionDto
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public string LessonName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string? Topic { get; set; }
    public bool IsAttendanceActive { get; set; }
    public DateTime? AttendanceActivatedAt { get; set; }
    public DateTime? AttendanceDeactivatedAt { get; set; }

    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
}

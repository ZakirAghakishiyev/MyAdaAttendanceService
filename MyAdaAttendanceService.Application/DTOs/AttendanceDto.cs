namespace MyAdaAttendanceService.Application.DTOs;

public class AttendanceDto
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int LessonId { get; set; }

    public int StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? FirstScanAt { get; set; }
    public DateTime? LastScanAt { get; set; }

    public bool IsManuallyAdjusted { get; set; }
    public string? InstructorNote { get; set; }
}

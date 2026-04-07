namespace MyAdaAttendanceService.Application.DTOs;

public class StudentAttendanceDto
{
    public int AttendanceId { get; set; }
    public int SessionId { get; set; }

    public DateTime SessionStartTime { get; set; }
    public DateTime SessionEndTime { get; set; }

    public string LessonName { get; set; } = string.Empty;
    public string LessonCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? FirstScanAt { get; set; }
    public DateTime? LastScanAt { get; set; }

    public bool IsManuallyAdjusted { get; set; }
    public string? InstructorNote { get; set; }
}

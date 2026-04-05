using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Application.DTOs;

public class StudentAttendanceDto
{
    public int AttendanceId { get; set; }

    public int SessionId { get; set; }
    public DateTime SessionStartTime { get; set; }
    public DateTime SessionEndTime { get; set; }

    public string LessonName { get; set; } = string.Empty;
    public string LessonCode { get; set; } = string.Empty;

    public AttendanceStatus Status { get; set; }

    public DateTime? FirstScanAt { get; set; }
    public string? InstructorNote { get; set; }
}

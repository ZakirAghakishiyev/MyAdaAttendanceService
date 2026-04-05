namespace MyAdaAttendanceService.Application.DTOs;

public class StudentLessonDto
{
    public int LessonId { get; set; }
    public string LessonName { get; set; } = string.Empty;
    public string LessonCode { get; set; } = string.Empty;

    public int TotalSessions { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
}

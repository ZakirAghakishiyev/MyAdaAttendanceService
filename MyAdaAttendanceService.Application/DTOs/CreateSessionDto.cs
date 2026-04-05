namespace MyAdaAttendanceService.Application.DTOs;

public class CreateSessionDto
{
    public int LessonId { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string? Topic { get; set; }
}

namespace MyAdaAttendanceService.Application.DTOs;

public class EnrollmentDto
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
}

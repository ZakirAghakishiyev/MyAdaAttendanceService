namespace MyAdaAttendanceService.Application.DTOs;

public class LessonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public int InstructorId { get; set; }

    public ICollection<SessionShortDto>? Sessions { get; set; }
}

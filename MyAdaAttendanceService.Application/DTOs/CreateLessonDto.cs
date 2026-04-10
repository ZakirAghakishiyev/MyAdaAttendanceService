namespace MyAdaAttendanceService.Application.DTOs;

public class CreateLessonDto
{
    /// <summary>Assigned instructor for this lesson (set by office; not derived from the caller).</summary>
    public int InstructorId { get; set; }

    public int RoomId { get; set; }
    public string Semester { get; set; } = string.Empty;
    public string CRN { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TimesPerWeek { get; set; }
    public int Capacity { get; set; }
}

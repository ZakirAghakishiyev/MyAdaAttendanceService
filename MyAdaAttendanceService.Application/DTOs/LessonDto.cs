using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.DTOs;

public class LessonDto
{
    public int Id { get; set; }

    /// <summary>Catalog title from the linked course (lessons do not store a separate name).</summary>
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid InstructorId { get; set; }
    public string InstructorDisplayName { get; set; } = string.Empty;
    public string InstructorEmail { get; set; } = string.Empty;

    public int AcademicYear { get; set; }
    public AcademicSemester Semester { get; set; }

    /// <summary>Five-character CRN: semester digit (1/2/3) plus four-digit sequence.</summary>
    public string CRN { get; set; } = string.Empty;

    /// <summary>Maximum enrollments allowed for this lesson.</summary>
    public int MaxCapacity { get; set; }

    public ICollection<SessionShortDto>? Sessions { get; set; }
}

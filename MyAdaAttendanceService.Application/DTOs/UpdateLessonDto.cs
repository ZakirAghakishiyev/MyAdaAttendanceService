using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.DTOs;

public class UpdateLessonDto
{
    /// <summary>Course this lesson is tied to (same semantics as <see cref="CreateLessonDto.CourseId"/>).</summary>
    public int CourseId { get; set; }

    public Guid InstructorId { get; set; }

    public int RoomId { get; set; }

    public int AcademicYear { get; set; }

    public AcademicSemester Semester { get; set; }

    public int MaxCapacity { get; set; }
}

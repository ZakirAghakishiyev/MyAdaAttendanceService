using System.ComponentModel.DataAnnotations;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Validation;

namespace MyAdaAttendanceService.Core.Entities;

public class Lesson
{
    public int Id { get; set; }

    [NonEmptyGuid]
    public Guid InstructorId { get; set; }

    [Range(0, int.MaxValue)]
    public int RoomId { get; set; }

    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    /// <summary>Calendar year the term applies to (e.g. 2026 for Spring 2026).</summary>
    [Range(2000, 2100)]
    public int AcademicYear { get; set; }

    public AcademicSemester Semester { get; set; }

    [Required]
    [StringLength(CrnFormatter.CrnLength)]
    public string CRN { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int MaxCapacity { get; set; }

    public ICollection<LessonEnrollment> Enrollments { get; set; } = new List<LessonEnrollment>();
    public ICollection<LessonSession> Sessions { get; set; } = new List<LessonSession>();
    public ICollection<LessonTime> LessonTimes { get; set; } = new List<LessonTime>();
}

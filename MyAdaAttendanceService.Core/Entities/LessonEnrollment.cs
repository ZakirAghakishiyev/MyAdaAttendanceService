using System.ComponentModel.DataAnnotations;
using MyAdaAttendanceService.Core.Validation;

namespace MyAdaAttendanceService.Core.Entities;

public class LessonEnrollment
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int LessonId { get; set; }

    [NonEmptyGuid]
    public Guid StudentId { get; set; }

    public Lesson? Lesson { get; set; }
}

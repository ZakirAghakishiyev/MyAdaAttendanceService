using System.ComponentModel.DataAnnotations;

namespace MyAdaAttendanceService.Core.Entities;

public class LessonEnrollment
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int LessonId { get; set; }

    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    public Lesson? Lesson { get; set; }
}

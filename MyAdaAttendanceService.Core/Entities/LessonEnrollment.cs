namespace MyAdaAttendanceService.Core.Entities;

public class LessonEnrollment
{
    public int Id { get; set; }

    public int LessonId { get; set; }
    public int StudentId { get; set; }

    public Lesson? Lesson { get; set; }
}

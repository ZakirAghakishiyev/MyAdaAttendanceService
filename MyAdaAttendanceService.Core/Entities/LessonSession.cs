namespace MyAdaAttendanceService.Core.Entities;

public class LessonSession
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public Lesson? Lesson { get; set; }
    public ICollection<SessionAttendance> Attendances { get; set; } = new List<SessionAttendance>();
}

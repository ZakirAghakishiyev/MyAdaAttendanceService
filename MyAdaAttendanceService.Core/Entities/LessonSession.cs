namespace MyAdaAttendanceService.Core.Entities;

public class LessonSession
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public string? Topic { get; set; }
    public bool IsAttendanceActive { get; set; }
    public DateTime? AttendanceActivatedAt { get; set; }
    public DateTime? AttendanceDeactivatedAt { get; set; }

    public Lesson? Lesson { get; set; }
    public ICollection<SessionAttendance> Attendances { get; set; } = new List<SessionAttendance>();
}

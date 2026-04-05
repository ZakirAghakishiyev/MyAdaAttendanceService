namespace MyAdaAttendanceService.Core.Entities;

public class SessionAttendance
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public int StudentId { get; set; }

    public AttendanceStatus Status { get; set; }

    public LessonSession? Session { get; set; }
}

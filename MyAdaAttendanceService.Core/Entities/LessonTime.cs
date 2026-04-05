namespace MyAdaAttendanceService.Core.Entities;

public class LessonTime
{
    public int Id { get; set; }

    public int LessonId { get; set; }
    public int TimeslotId { get; set; }

    public Lesson? Lesson { get; set; }
    public Timeslot? Timeslot { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace MyAdaAttendanceService.Core.Entities;

public class LessonTime
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int LessonId { get; set; }

    [Range(1, int.MaxValue)]
    public int TimeslotId { get; set; }

    public Lesson? Lesson { get; set; }
    public Timeslot? Timeslot { get; set; }
}

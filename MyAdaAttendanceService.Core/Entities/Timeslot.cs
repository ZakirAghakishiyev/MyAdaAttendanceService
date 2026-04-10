using System.ComponentModel.DataAnnotations;

namespace MyAdaAttendanceService.Core.Entities;

public class Timeslot
{
    public int Id { get; set; }

    [Required]
    [StringLength(16)]
    public string Day { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public ICollection<LessonTime> LessonTimes { get; set; } = new List<LessonTime>();
}

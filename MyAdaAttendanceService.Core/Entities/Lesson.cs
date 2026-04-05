namespace MyAdaAttendanceService.Core.Entities;

public class Lesson
{
    public int Id { get; set; }

    public int InstructorId { get; set; }
    public int RoomId { get; set; }

    public string Semester { get; set; } = string.Empty;
    public string CRN { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public int Credits { get; set; }
    public int TimesPerWeek { get; set; }
    public int Capacity { get; set; }

    public ICollection<LessonEnrollment> Enrollments { get; set; } = new List<LessonEnrollment>();
    public ICollection<LessonSession> Sessions { get; set; } = new List<LessonSession>();
    public ICollection<LessonTime> LessonTimes { get; set; } = new List<LessonTime>();
}

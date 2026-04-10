using System.ComponentModel.DataAnnotations;

namespace MyAdaAttendanceService.Core.Entities;

public class Lesson
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int InstructorId { get; set; }

    [Range(0, int.MaxValue)]
    public int RoomId { get; set; }

    [Required]
    [StringLength(32)]
    public string Semester { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string CRN { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Credits { get; set; }

    [Range(0, int.MaxValue)]
    public int TimesPerWeek { get; set; }

    [Range(0, int.MaxValue)]
    public int Capacity { get; set; }

    public ICollection<LessonEnrollment> Enrollments { get; set; } = new List<LessonEnrollment>();
    public ICollection<LessonSession> Sessions { get; set; } = new List<LessonSession>();
    public ICollection<LessonTime> LessonTimes { get; set; } = new List<LessonTime>();
}

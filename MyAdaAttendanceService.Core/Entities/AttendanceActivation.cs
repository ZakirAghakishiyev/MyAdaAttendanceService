using System.ComponentModel.DataAnnotations;

namespace MyAdaAttendanceService.Core.Entities;

public class AttendanceActivation
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public int? CreatedByInstructorId { get; set; }

    public LessonSession? Session { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace MyAdaAttendanceService.Core.Entities;

public class AttendanceActivation
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }

    /// <summary>1 = first scan window, 2 = second (late) window. Each period has its own activation and QR.</summary>
    [Range(1, 2)]
    public byte Round { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedByInstructorId { get; set; }

    public LessonSession? Session { get; set; }
}

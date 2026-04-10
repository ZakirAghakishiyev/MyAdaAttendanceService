namespace MyAdaAttendanceService.Core.Entities;

public class SessionAttendance
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public int StudentId { get; set; }

    public AttendanceStatus Status { get; set; }
    public DateTime? MarkedAt { get; set; }
    public AttendanceMarkedSource MarkedSource { get; set; } = AttendanceMarkedSource.QR;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? FirstScanAt { get; set; }
    public DateTime? LastScanAt { get; set; }

    public bool IsManuallyAdjusted { get; set; }
    public string? InstructorNote { get; set; }

    public LessonSession? Session { get; set; }
}

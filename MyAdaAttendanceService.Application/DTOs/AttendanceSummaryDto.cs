namespace MyAdaAttendanceService.Application.DTOs;

public class AttendanceSummaryDto
{
    public int SessionId { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
}

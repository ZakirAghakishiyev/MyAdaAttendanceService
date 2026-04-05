namespace MyAdaAttendanceService.Application.DTOs;

public class UpdateSessionDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string? Topic { get; set; }
}

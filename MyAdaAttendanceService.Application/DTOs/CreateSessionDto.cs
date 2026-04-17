namespace MyAdaAttendanceService.Application.DTOs;

/// <summary>Session times and optional topic; lesson comes from the URL path.</summary>
public class CreateSessionDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Topic { get; set; }
}

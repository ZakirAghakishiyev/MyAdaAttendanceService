namespace MyAdaAttendanceService.Application.DTOs;

public class SessionShortDto
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public string? Topic { get; set; }
    public bool IsAttendanceActive { get; set; }
}

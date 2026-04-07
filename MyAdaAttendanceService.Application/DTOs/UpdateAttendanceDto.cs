namespace MyAdaAttendanceService.Application.DTOs;

public class UpdateAttendanceDto
{
    public string Status { get; set; } = string.Empty;
    public string? InstructorNote { get; set; }
}

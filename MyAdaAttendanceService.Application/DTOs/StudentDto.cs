namespace MyAdaAttendanceService.Application.DTOs;

public class StudentDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string? Email { get; set; }
}

namespace MyAdaAttendanceService.Application.DTOs;

public class CourseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TimesPerWeek { get; set; }
}

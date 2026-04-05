using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Application.DTOs;

public class UpdateAttendanceDto
{
    public AttendanceStatus Status { get; set; }

    public string? InstructorNote { get; set; }
}

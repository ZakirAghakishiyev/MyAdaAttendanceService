using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Application.DTOs;

public class AdminAttendanceCorrectionDto
{
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
}
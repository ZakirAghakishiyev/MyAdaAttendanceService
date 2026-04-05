using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IAdminAttendanceService
{
    Task FixAttendanceAsync(int attendanceId, UpdateAttendanceDto dto);

    Task DeleteAttendanceAsync(int attendanceId);
}
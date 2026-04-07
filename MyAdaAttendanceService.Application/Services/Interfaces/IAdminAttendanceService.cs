using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;


public interface IAdminAttendanceService
{
    Task<AttendanceDto> FixAttendanceAsync(int attendanceId, AdminAttendanceCorrectionDto dto);

    Task DeleteAttendanceAsync(int attendanceId);
}
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class AdminAttendanceService : IAdminAttendanceService
{
    private readonly ISessionAttendanceRepository _attendanceRepository;

    public AdminAttendanceService(ISessionAttendanceRepository attendanceRepository)
    {
        _attendanceRepository = attendanceRepository;
    }

    public async Task FixAttendanceAsync(int attendanceId, UpdateAttendanceDto dto)
    {
        var attendance = await _attendanceRepository.GetByIdAsync(attendanceId);
        attendance.Status = dto.Status;
        await _attendanceRepository.UpdateAsync(attendance);
    }

    public async Task DeleteAttendanceAsync(int attendanceId)
    {
        var attendance = await _attendanceRepository.GetByIdAsync(attendanceId);
        await _attendanceRepository.RemoveAsync(attendance);
    }
}
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class AdminAttendanceService : IAdminAttendanceService
{
    private readonly ISessionAttendanceRepository _attendanceRepository;
    private readonly ILessonSessionRepository _sessionRepository;

    public AdminAttendanceService(
        ISessionAttendanceRepository attendanceRepository,
        ILessonSessionRepository sessionRepository)
    {
        _attendanceRepository = attendanceRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<AttendanceDto> FixAttendanceAsync(int attendanceId, AdminAttendanceCorrectionDto dto)
    {
        var attendance = await _attendanceRepository.GetByIdAsync(attendanceId);

        if (!Enum.TryParse<AttendanceStatus>(dto.Status, true, out var status))
            throw new ArgumentException($"Invalid attendance status: {dto.Status}");

        attendance.Status = status;
        attendance.InstructorNote = dto.Note;
        attendance.IsManuallyAdjusted = true;

        await _attendanceRepository.UpdateAsync(attendance);

        var session = await _sessionRepository.GetByIdAsync(attendance.SessionId);

        return new AttendanceDto
        {
            Id = attendance.Id,
            SessionId = attendance.SessionId,
            LessonId = session.LessonId,
            StudentId = attendance.StudentId,
            Status = attendance.Status.ToString(),
            FirstScanAt = attendance.FirstScanAt,
            LastScanAt = attendance.LastScanAt,
            IsManuallyAdjusted = attendance.IsManuallyAdjusted,
            InstructorNote = attendance.InstructorNote
        };
    }

    public async Task DeleteAttendanceAsync(int attendanceId)
    {
        var attendance = await _attendanceRepository.GetByIdAsync(attendanceId);
        await _attendanceRepository.RemoveAsync(attendance);
    }
}

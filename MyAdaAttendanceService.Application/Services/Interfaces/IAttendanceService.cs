using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IAttendanceService
{
    Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAsync(int instructorId, int sessionId);

    Task<AttendanceSummaryDto> GetSessionAttendanceSummaryAsync(int instructorId, int sessionId);

    Task<QrScanResponseDto> MarkAttendanceByQrAsync(int studentId, QrScanRequestDto dto);

    Task<AttendanceDto> UpdateAttendanceAsync(int instructorId, int attendanceId, UpdateAttendanceDto dto);

    Task BulkMarkAbsentAsync(int instructorId, int sessionId);
}

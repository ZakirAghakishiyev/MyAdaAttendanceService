using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceActivationResultDto> ActivateAttendanceAsync(int instructorId, int sessionId);
    Task<AttendanceActivationResultDto> DeactivateAttendanceAsync(int instructorId, int sessionId);
    Task<QrTokenResponseDto> IssueQrTokenAsync(int instructorId, int sessionId);
    Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAsync(int instructorId, int sessionId);

    Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAdminAsync(int sessionId);

    Task<AttendanceSummaryDto> GetSessionAttendanceSummaryAsync(int instructorId, int sessionId);

    Task<QrScanResponseDto> MarkAttendanceByQrAsync(int studentId, QrScanRequestDto dto);

    Task<AttendanceDto> UpdateAttendanceAsync(int instructorId, int sessionId, int studentId, UpdateAttendanceDto dto);

    Task FinalizeAttendanceAsync(int instructorId, int sessionId);

    Task BulkMarkAbsentAsync(int instructorId, int sessionId);
}

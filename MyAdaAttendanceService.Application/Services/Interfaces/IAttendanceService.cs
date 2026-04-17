using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceActivationResultDto> ActivateAttendanceAsync(Guid instructorId, int sessionId);
    Task<AttendanceActivationResultDto> DeactivateAttendanceAsync(Guid instructorId, int sessionId);
    Task<QrTokenResponseDto> IssueQrTokenAsync(Guid instructorId, int sessionId);
    Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAsync(Guid instructorId, int sessionId);

    Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAdminAsync(int sessionId);

    Task<AttendanceSummaryDto> GetSessionAttendanceSummaryAsync(Guid instructorId, int sessionId);

    Task<QrScanResponseDto> MarkAttendanceByQrAsync(QrScanRequestDto dto);

    Task<AttendanceDto> UpdateAttendanceAsync(Guid instructorId, int sessionId, Guid studentId, UpdateAttendanceDto dto);

    Task FinalizeAttendanceAsync(Guid instructorId, int sessionId);

    Task BulkMarkAbsentAsync(Guid instructorId, int sessionId);
}

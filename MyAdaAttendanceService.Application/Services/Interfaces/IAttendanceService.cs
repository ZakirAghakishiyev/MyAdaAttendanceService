using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceActivationResultDto> ActivateAttendanceForRoundAsync(Guid instructorId, int sessionId, int round);

    Task<AttendanceActivationResultDto> DeactivateAttendanceForRoundAsync(Guid instructorId, int sessionId, int round);
    Task<QrTokenResponseDto> IssueQrTokenAsync(Guid instructorId, int sessionId);
    Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAsync(Guid instructorId, int sessionId);

    Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAdminAsync(int sessionId);

    Task<AttendanceSummaryDto> GetSessionAttendanceSummaryAsync(Guid instructorId, int sessionId);

    Task<QrScanResponseDto> MarkAttendanceByQrAsync(QrScanRequestDto dto);

    Task<AttendanceDto> UpdateAttendanceAsync(Guid instructorId, int sessionId, Guid studentId, UpdateAttendanceDto dto);

    Task FinalizeAttendanceAsync(Guid instructorId, int sessionId);

    Task BulkMarkAbsentAsync(Guid instructorId, int sessionId);
}

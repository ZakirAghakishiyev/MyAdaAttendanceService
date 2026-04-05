using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IAttendanceService
{
    // Instructor view
    Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAsync(int instructorId, int sessionId);

    // QR scan (student)
    Task MarkAttendanceByQrAsync(int studentId, int sessionId);

    // Manual adjustment (Instructor)
    Task UpdateAttendanceAsync(int instructorId, int attendanceId, UpdateAttendanceDto dto);

    // Optional: bulk operations
    Task BulkMarkAbsentAsync(int instructorId, int sessionId);
}

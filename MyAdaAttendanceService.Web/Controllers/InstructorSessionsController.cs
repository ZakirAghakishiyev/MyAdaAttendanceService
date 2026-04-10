using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/instructors/me/sessions")]
public class InstructorSessionsController : ApiControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public InstructorSessionsController(
        IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("{sessionId:int}/attendance/activate")]
    public async Task<IActionResult> ActivateAttendance(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.ActivateAttendanceAsync(instructorId, sessionId));
    }

    [HttpPost("{sessionId:int}/attendance/deactivate")]
    public async Task<IActionResult> DeactivateAttendance(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.DeactivateAttendanceAsync(instructorId, sessionId));
    }

    [HttpPost("{sessionId:int}/qr-token")]
    public async Task<IActionResult> IssueQrToken(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.IssueQrTokenAsync(instructorId, sessionId));
    }

    [HttpGet("{sessionId:int}/attendance")]
    public async Task<IActionResult> GetAttendance(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.GetSessionAttendanceAsync(instructorId, sessionId));
    }

    [HttpGet("{sessionId:int}/attendance/summary")]
    public async Task<IActionResult> GetAttendanceSummary(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.GetSessionAttendanceSummaryAsync(instructorId, sessionId));
    }

    [HttpPatch("{sessionId:int}/attendance/{studentId:int}")]
    public async Task<IActionResult> UpdateAttendance(
        int sessionId,
        int studentId,
        [FromBody] UpdateAttendanceDto dto)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.UpdateAttendanceAsync(instructorId, sessionId, studentId, dto));
    }

    [HttpPost("{sessionId:int}/attendance/finalize")]
    public async Task<IActionResult> FinalizeAttendance(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.FinalizeAttendanceAsync(instructorId, sessionId));
    }

    [HttpPost("{sessionId:int}/attendance/bulk-absent")]
    public async Task<IActionResult> BulkMarkAbsent(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.BulkMarkAbsentAsync(instructorId, sessionId));
    }
}

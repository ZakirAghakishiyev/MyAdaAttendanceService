using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/instructors/me/sessions")]
public class InstructorSessionsController : ApiControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IAttendanceService _attendanceService;

    public InstructorSessionsController(
        ISessionService sessionService,
        IAttendanceService attendanceService)
    {
        _sessionService = sessionService;
        _attendanceService = attendanceService;
    }

    [HttpPost("{sessionId:int}/qr-token")]
    public async Task<IActionResult> ActivateQr(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        var result = await _sessionService.ActivateAttendanceAsync(instructorId, sessionId);
        return Ok(result);
    }

    [HttpDelete("{sessionId:int}/qr-token")]
    public async Task<IActionResult> DeactivateQr(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        var result = await _sessionService.DeactivateAttendanceAsync(instructorId, sessionId);
        return Ok(result);
    }

    [HttpGet("{sessionId:int}/attendance")]
    public async Task<IActionResult> GetAttendance(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        var records = await _attendanceService.GetSessionAttendanceAsync(instructorId, sessionId);
        return Ok(records);
    }

    [HttpGet("{sessionId:int}/attendance/summary")]
    public async Task<IActionResult> GetAttendanceSummary(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        var summary = await _attendanceService.GetSessionAttendanceSummaryAsync(instructorId, sessionId);
        return Ok(summary);
    }

    [HttpPatch("{sessionId:int}/attendance/{attendanceId:int}")]
    public async Task<IActionResult> UpdateAttendance(
        int sessionId,
        int attendanceId,
        [FromBody] UpdateAttendanceDto dto)
    {
        var instructorId = GetCurrentUserId();
        var updated = await _attendanceService.UpdateAttendanceAsync(instructorId, attendanceId, dto);
        return Ok(updated);
    }

    [HttpPost("{sessionId:int}/attendance/bulk-absent")]
    public async Task<IActionResult> BulkMarkAbsent(int sessionId)
    {
        var instructorId = GetCurrentUserId();
        await _attendanceService.BulkMarkAbsentAsync(instructorId, sessionId);
        return NoContent();
    }
}

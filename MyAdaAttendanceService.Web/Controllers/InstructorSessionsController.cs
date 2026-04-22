using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/instructors/{instructorId:guid}/sessions")]
public class InstructorSessionsController : ApiControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public InstructorSessionsController(
        IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("{sessionId:int}/attendance/activate/{round:int}")]
    public Task<IActionResult> ActivateAttendanceForRound(
        Guid instructorId,
        int sessionId,
        int round) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _attendanceService.ActivateAttendanceForRoundAsync(instructorId, sessionId, round);
        });

    [HttpPost("{sessionId:int}/attendance/deactivate/{round:int}")]
    public Task<IActionResult> DeactivateAttendanceForRound(
        Guid instructorId,
        int sessionId,
        int round) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _attendanceService.DeactivateAttendanceForRoundAsync(instructorId, sessionId, round);
        });

    [HttpPost("{sessionId:int}/qr-token")]
    public Task<IActionResult> IssueQrToken(Guid instructorId, int sessionId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _attendanceService.IssueQrTokenAsync(instructorId, sessionId);
        });

    [HttpGet("{sessionId:int}/attendance")]
    public Task<IActionResult> GetAttendance(Guid instructorId, int sessionId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _attendanceService.GetSessionAttendanceAsync(instructorId, sessionId);
        });

    [HttpGet("{sessionId:int}/attendance/summary")]
    public Task<IActionResult> GetAttendanceSummary(Guid instructorId, int sessionId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _attendanceService.GetSessionAttendanceSummaryAsync(instructorId, sessionId);
        });

    [HttpPatch("{sessionId:int}/attendance/{studentId:guid}")]
    public Task<IActionResult> UpdateAttendance(
        Guid instructorId,
        int sessionId,
        Guid studentId,
        [FromBody] UpdateAttendanceDto dto) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _attendanceService.UpdateAttendanceAsync(instructorId, sessionId, studentId, dto);
        });

    [HttpPost("{sessionId:int}/attendance/finalize")]
    public Task<IActionResult> FinalizeAttendance(Guid instructorId, int sessionId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            await _attendanceService.FinalizeAttendanceAsync(instructorId, sessionId);
        });

    [HttpPost("{sessionId:int}/attendance/bulk-absent")]
    public Task<IActionResult> BulkMarkAbsent(Guid instructorId, int sessionId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            await _attendanceService.BulkMarkAbsentAsync(instructorId, sessionId);
        });
}

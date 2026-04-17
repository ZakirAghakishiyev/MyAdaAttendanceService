using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/students/{studentId:guid}/attendance")]
public class AttendanceController : ApiControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("qr/scan")]
    public Task<IActionResult> ScanQr(Guid studentId, [FromBody] QrScanRequestDto dto) =>
        HandleAsync(async () =>
        {
            if (dto.StudentId == Guid.Empty)
                dto.StudentId = studentId;
            else if (studentId != Guid.Empty && dto.StudentId != studentId)
                throw new ArgumentException("Route student id and payload student id must match.");

            EnsureAuthenticatedUserMatches(dto.StudentId);
            return await _attendanceService.MarkAttendanceByQrAsync(dto);
        });
}

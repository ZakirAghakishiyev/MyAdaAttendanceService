using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/attendance")]
public class AttendanceController : ApiControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("qr/scan")]
    public async Task<IActionResult> ScanQr([FromBody] QrScanRequestDto dto)
    {
        var studentId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.MarkAttendanceByQrAsync(studentId, dto));
    }
}

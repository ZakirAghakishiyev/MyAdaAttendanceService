using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/students/me")]
public class StudentController : ApiControllerBase
{
    private readonly IStudentAttendanceService _studentAttendanceService;
    private readonly IAttendanceService _attendanceService;

    public StudentController(
        IStudentAttendanceService studentAttendanceService,
        IAttendanceService attendanceService)
    {
        _studentAttendanceService = studentAttendanceService;
        _attendanceService = attendanceService;
    }

    [HttpGet("enrollments")]
    public async Task<IActionResult> GetMyEnrollments()
    {
        var studentId = GetCurrentUserId();
        return await HandleAsync(() => _studentAttendanceService.GetMyLessonsAsync(studentId));
    }

    [HttpGet("lessons/{lessonId:int}/attendance")]
    public async Task<IActionResult> GetMyAttendanceByLesson(int lessonId)
    {
        var studentId = GetCurrentUserId();
        return await HandleAsync(() => _studentAttendanceService.GetMyAttendanceByLessonAsync(studentId, lessonId));
    }

    [HttpPost("attendance/scan")]
    public async Task<IActionResult> ScanAttendance([FromBody] QrScanRequestDto dto)
    {
        var studentId = GetCurrentUserId();
        return await HandleAsync(() => _attendanceService.MarkAttendanceByQrAsync(studentId, dto));
    }
}

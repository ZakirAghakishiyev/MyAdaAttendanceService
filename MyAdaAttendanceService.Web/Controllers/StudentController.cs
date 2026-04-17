using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/students/{studentId:guid}")]
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
    public Task<IActionResult> GetMyEnrollments(Guid studentId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(studentId);
            return await _studentAttendanceService.GetMyLessonsAsync(studentId);
        });

    [HttpGet("lessons/{lessonId:int}/attendance")]
    public Task<IActionResult> GetMyAttendanceByLesson(Guid studentId, int lessonId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(studentId);
            return await _studentAttendanceService.GetMyAttendanceByLessonAsync(studentId, lessonId);
        });

    [HttpPost("attendance/scan")]
    public Task<IActionResult> ScanAttendance(Guid studentId, [FromBody] QrScanRequestDto dto) =>
        HandleAsync(async () =>
        {
            if (dto.StudentId == Guid.Empty)
                dto.StudentId = studentId;
            else if (dto.StudentId != studentId)
                throw new ArgumentException("Route student id and payload student id must match.");

            EnsureAuthenticatedUserMatches(dto.StudentId);
            return await _attendanceService.MarkAttendanceByQrAsync(dto);
        });
}

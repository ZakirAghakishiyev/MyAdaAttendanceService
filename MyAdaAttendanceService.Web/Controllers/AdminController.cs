using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/admin")]
public class AdminController : ApiControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly ISessionService _sessionService;
    private readonly IAttendanceService _attendanceService;
    private readonly IAdminAttendanceService _adminAttendanceService;

    public AdminController(
        ILessonService lessonService,
        ISessionService sessionService,
        IAttendanceService attendanceService,
        IAdminAttendanceService adminAttendanceService)
    {
        _lessonService = lessonService;
        _sessionService = sessionService;
        _attendanceService = attendanceService;
        _adminAttendanceService = adminAttendanceService;
    }

    [HttpGet("instructors/{instructorId:int}/lessons")]
    public async Task<IActionResult> GetLessonsByInstructor(int instructorId)
    {
        return await HandleAsync(() => _lessonService.GetMyLessonsAsync(instructorId));
    }

    [HttpGet("lessons")]
    public async Task<IActionResult> GetAllLessons()
    {
        return await HandleAsync(() => _lessonService.GetAllLessonsAsync());
    }

    [HttpGet("lessons/{lessonId:int}")]
    public async Task<IActionResult> GetLessonById(int lessonId)
    {
        return await HandleAsync(() => _lessonService.GetLessonByIdAsync(lessonId));
    }

    /// <summary>Office/registrar: create a lesson and assign an instructor via <see cref="CreateLessonDto.InstructorId"/>.</summary>
    [HttpPost("lessons")]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonDto dto)
    {
        return await HandleAsync(
            () => _lessonService.CreateLessonAsync(dto),
            lesson => Created($"/api/admin/lessons/{lesson.Id}", lesson));
    }

    [HttpGet("lessons/{lessonId:int}/sessions")]
    public async Task<IActionResult> GetSessionsByLesson(int lessonId)
    {
        return await HandleAsync(() => _sessionService.GetSessionsByLessonAdminAsync(lessonId));
    }

    [HttpGet("sessions/{sessionId:int}/attendance")]
    public async Task<IActionResult> GetSessionAttendance(int sessionId)
    {
        return await HandleAsync(() => _attendanceService.GetSessionAttendanceAdminAsync(sessionId));
    }

    [HttpPatch("sessions/{sessionId:int}/attendance/{attendanceId:int}")]
    public async Task<IActionResult> CorrectAttendance(int sessionId, int attendanceId, [FromBody] AdminAttendanceCorrectionDto dto)
    {
        return await HandleAsync(() => _adminAttendanceService.FixAttendanceAsync(attendanceId, dto));
    }

    [HttpDelete("sessions/{sessionId:int}/attendance/{attendanceId:int}")]
    public async Task<IActionResult> DeleteAttendance(int sessionId, int attendanceId)
    {
        return await HandleAsync(() => _adminAttendanceService.DeleteAttendanceAsync(attendanceId));
    }
}

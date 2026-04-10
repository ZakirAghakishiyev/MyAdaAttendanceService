using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/instructors/me/lessons")]
public class InstructorLessonsController : ApiControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly ISessionService _sessionService;

    public InstructorLessonsController(
        ILessonService lessonService,
        ISessionService sessionService)
    {
        _lessonService = lessonService;
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyLessons()
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _lessonService.GetMyLessonsAsync(instructorId));
    }

    [HttpGet("{lessonId:int}")]
    public async Task<IActionResult> GetMyLesson(int lessonId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _lessonService.GetMyLessonByIdAsync(instructorId, lessonId));
    }

    [HttpGet("{lessonId:int}/sessions")]
    public async Task<IActionResult> GetSessions(int lessonId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _sessionService.GetSessionsByLessonAsync(instructorId, lessonId));
    }

    [HttpGet("{lessonId:int}/sessions/{sessionId:int}")]
    public async Task<IActionResult> GetSession(int lessonId, int sessionId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _sessionService.GetSessionByIdAsync(instructorId, sessionId));
    }

    [HttpPost("{lessonId:int}/sessions")]
    public async Task<IActionResult> CreateSession(int lessonId, [FromBody] CreateSessionDto dto)
    {
        var instructorId = GetCurrentUserId();
        dto.LessonId = lessonId;
        return await HandleAsync(
            () => _sessionService.CreateSessionAsync(instructorId, dto),
            session => CreatedAtAction(nameof(GetSession), new { lessonId, sessionId = session.Id }, session));
    }

    [HttpPatch("{lessonId:int}/sessions/{sessionId:int}")]
    public async Task<IActionResult> UpdateSession(int lessonId, int sessionId, [FromBody] UpdateSessionDto dto)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _sessionService.UpdateSessionAsync(instructorId, sessionId, dto));
    }

    [HttpDelete("{lessonId:int}/sessions/{sessionId:int}")]
    public async Task<IActionResult> DeleteSession(int lessonId, int sessionId)
    {
        var instructorId = GetCurrentUserId();
        return await HandleAsync(() => _sessionService.DeleteSessionAsync(instructorId, sessionId));
    }
}

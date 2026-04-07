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
        var lessons = await _lessonService.GetMyLessonsAsync(instructorId);
        return Ok(lessons);
    }

    [HttpGet("{lessonId:int}")]
    public async Task<IActionResult> GetMyLesson(int lessonId)
    {
        var instructorId = GetCurrentUserId();
        var lesson = await _lessonService.GetMyLessonByIdAsync(instructorId, lessonId);
        return Ok(lesson);
    }

    [HttpGet("{lessonId:int}/sessions")]
    public async Task<IActionResult> GetSessions(int lessonId)
    {
        var instructorId = GetCurrentUserId();
        var sessions = await _sessionService.GetSessionsByLessonAsync(instructorId, lessonId);
        return Ok(sessions);
    }

    [HttpGet("{lessonId:int}/sessions/{sessionId:int}")]
    public async Task<IActionResult> GetSession(int lessonId, int sessionId)
    {
        var instructorId = GetCurrentUserId();
        var session = await _sessionService.GetSessionByIdAsync(instructorId, sessionId);
        return Ok(session);
    }

    [HttpPost("{lessonId:int}/sessions")]
    public async Task<IActionResult> CreateSession(int lessonId, [FromBody] CreateSessionDto dto)
    {
        var instructorId = GetCurrentUserId();
        dto.LessonId = lessonId;
        var session = await _sessionService.CreateSessionAsync(instructorId, dto);
        return CreatedAtAction(nameof(GetSession), new { lessonId, sessionId = session.Id }, session);
    }

    [HttpPatch("{lessonId:int}/sessions/{sessionId:int}")]
    public async Task<IActionResult> UpdateSession(int lessonId, int sessionId, [FromBody] UpdateSessionDto dto)
    {
        var instructorId = GetCurrentUserId();
        var session = await _sessionService.UpdateSessionAsync(instructorId, sessionId, dto);
        return Ok(session);
    }

    [HttpDelete("{lessonId:int}/sessions/{sessionId:int}")]
    public async Task<IActionResult> DeleteSession(int lessonId, int sessionId)
    {
        var instructorId = GetCurrentUserId();
        await _sessionService.DeleteSessionAsync(instructorId, sessionId);
        return NoContent();
    }
}

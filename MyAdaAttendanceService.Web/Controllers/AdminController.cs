using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/admin")]
public class AdminController : ApiControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly ICourseService _courseService;
    private readonly ISessionService _sessionService;
    private readonly IAttendanceService _attendanceService;
    private readonly IAdminAttendanceService _adminAttendanceService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IExternalUserDirectoryService _externalUserDirectoryService;

    public AdminController(
        ILessonService lessonService,
        ICourseService courseService,
        ISessionService sessionService,
        IAttendanceService attendanceService,
        IAdminAttendanceService adminAttendanceService,
        IEnrollmentService enrollmentService,
        IExternalUserDirectoryService externalUserDirectoryService)
    {
        _lessonService = lessonService;
        _courseService = courseService;
        _sessionService = sessionService;
        _attendanceService = attendanceService;
        _adminAttendanceService = adminAttendanceService;
        _enrollmentService = enrollmentService;
        _externalUserDirectoryService = externalUserDirectoryService;
    }

    [HttpPost("users/sync")]
    public async Task<IActionResult> SyncUsers([FromQuery] string[]? roles)
    {
        return await HandleAsync(() => _externalUserDirectoryService.SyncUsersAsync(roles));
    }

    [HttpGet("users/roles/{role}")]
    public async Task<IActionResult> GetUsersByRole(string role)
    {
        return await HandleAsync(() => _externalUserDirectoryService.GetUsersByRoleAsync(role));
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetAllCourses()
    {
        return await HandleAsync(() => _courseService.GetAllAsync());
    }

    [HttpGet("courses/{courseId:int}")]
    public async Task<IActionResult> GetCourseById(int courseId)
    {
        return await HandleAsync(() => _courseService.GetByIdAsync(courseId));
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        return await HandleAsync(
            () => _courseService.CreateAsync(dto),
            course => Created($"/api/admin/courses/{course.Id}", course));
    }

    [HttpPut("courses/{courseId:int}")]
    public async Task<IActionResult> UpdateCourse(int courseId, [FromBody] UpdateCourseDto dto)
    {
        return await HandleAsync(() => _courseService.UpdateAsync(courseId, dto));
    }

    [HttpDelete("courses/{courseId:int}")]
    public async Task<IActionResult> DeleteCourse(int courseId)
    {
        return await HandleAsync(() => _courseService.DeleteAsync(courseId));
    }

    [HttpGet("courses/{courseId:int}/lessons")]
    public async Task<IActionResult> GetLessonsByCourse(int courseId)
    {
        return await HandleAsync(() => _lessonService.GetLessonsByCourseIdAsync(courseId));
    }

    [HttpGet("instructors/{instructorId:guid}/lessons")]
    public async Task<IActionResult> GetLessonsByInstructor(Guid instructorId)
    {
        return await HandleAsync(() => _lessonService.GetMyLessonsAsync(instructorId));
    }

    [HttpGet("lessons")]
    public async Task<IActionResult> GetAllLessons()
    {
        return await HandleAsync(() => _lessonService.GetAllLessonsAsync());
    }

    [HttpGet("lessons/by-term")]
    public async Task<IActionResult> GetLessonsByTerm([FromQuery] int academicYear, [FromQuery] AcademicSemester semester)
    {
        return await HandleAsync(() => _lessonService.GetLessonsByAcademicTermAsync(academicYear, semester));
    }

    /// <summary>Scheduling service: lesson id, instructor user id, enrollment count, times per week, and course code/title.</summary>
    [HttpGet("lessons/scheduling")]
    public async Task<IActionResult> GetLessonsForScheduling()
    {
        return await HandleAsync(() => _lessonService.GetLessonsForSchedulingAsync());
    }

    [HttpGet("lessons/{lessonId:int}")]
    public async Task<IActionResult> GetLessonById(int lessonId)
    {
        return await HandleAsync(() => _lessonService.GetLessonByIdAsync(lessonId));
    }

    /// <summary>Office/registrar: create a lesson for an existing course (<see cref="CreateLessonDto.CourseId"/>) and assign an instructor via <see cref="CreateLessonDto.InstructorId"/>.</summary>
    [HttpPost("lessons")]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonDto dto)
    {
        return await HandleAsync(
            () => _lessonService.CreateLessonAsync(dto),
            lesson => Created($"/api/admin/lessons/{lesson.Id}", lesson));
    }

    [HttpPut("lessons/{lessonId:int}")]
    public async Task<IActionResult> UpdateLesson(int lessonId, [FromBody] UpdateLessonDto dto)
    {
        return await HandleAsync(() => _lessonService.UpdateLessonAsync(lessonId, dto));
    }

    [HttpDelete("lessons/{lessonId:int}")]
    public async Task<IActionResult> DeleteLesson(int lessonId)
    {
        return await HandleAsync(() => _lessonService.DeleteLessonAsync(lessonId));
    }

    [HttpGet("lessons/{lessonId:int}/enrollments")]
    public async Task<IActionResult> GetEnrollmentsByLesson(int lessonId)
    {
        return await HandleAsync(() => _enrollmentService.GetEnrollmentsByLessonAsync(lessonId));
    }

    [HttpPost("lessons/{lessonId:int}/enrollments")]
    public async Task<IActionResult> CreateEnrollment(int lessonId, [FromBody] CreateEnrollmentDto dto)
    {
        return await HandleAsync(
            () => _enrollmentService.CreateEnrollmentAsync(lessonId, dto),
            enrollment => Created($"/api/admin/lessons/{lessonId}/enrollments/{enrollment.Id}", enrollment));
    }

    [HttpGet("lessons/{lessonId:int}/sessions")]
    public async Task<IActionResult> GetSessionsByLesson(int lessonId)
    {
        return await HandleAsync(() => _sessionService.GetSessionsByLessonAdminAsync(lessonId));
    }

    /// <summary>Generate sessions between two dates from a weekly day/time pattern; skips duplicates.</summary>
    [HttpPost("lessons/{lessonId:int}/sessions/generate")]
    public async Task<IActionResult> GenerateSessionsForLesson(int lessonId, [FromBody] BulkGenerateSessionsDto dto)
    {
        return await HandleAsync(() => _sessionService.BulkGenerateSessionsAdminAsync(lessonId, dto));
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

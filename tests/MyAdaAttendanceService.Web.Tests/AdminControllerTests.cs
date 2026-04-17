using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Web.Controllers;

namespace MyAdaAttendanceService.Web.Tests;

public class AdminControllerTests
{
    private static readonly Guid InstructorId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task GetAllLessons_ReturnsOkWithLessons()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.GetAllLessonsAsync())
            .ReturnsAsync(new List<LessonDto>
            {
                new() { Id = 1, Name = "Algorithms", Code = "CSE201", InstructorId = InstructorId }
            });

        var sessionService = new Mock<ISessionService>();
        var attendanceService = new Mock<IAttendanceService>();
        var adminAttendanceService = new Mock<IAdminAttendanceService>();
        var controller = new AdminController(
            lessonService.Object,
            new Mock<ICourseService>().Object,
            sessionService.Object,
            attendanceService.Object,
            adminAttendanceService.Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetAllLessons();

        var ok = Assert.IsType<OkObjectResult>(response);
        var lessons = Assert.IsAssignableFrom<IEnumerable<LessonDto>>(ok.Value);
        Assert.Single(lessons);
    }

    [Fact]
    public async Task GetLessonById_ReturnsNotFound_WhenServiceThrowsKeyNotFound()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.GetLessonByIdAsync(404))
            .ThrowsAsync(new KeyNotFoundException("Lesson 404 not found."));

        var controller = new AdminController(
            lessonService.Object,
            new Mock<ICourseService>().Object,
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetLessonById(404);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task GetSessionsByLesson_ReturnsOk_WhenServiceSucceeds()
    {
        var sessionService = new Mock<ISessionService>();
        sessionService
            .Setup(x => x.GetSessionsByLessonAdminAsync(102))
            .ReturnsAsync(new List<SessionDto>
            {
                new() { Id = 1, LessonId = 102, LessonName = "Algorithms", StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) }
            });

        var controller = new AdminController(
            new Mock<ILessonService>().Object,
            new Mock<ICourseService>().Object,
            sessionService.Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetSessionsByLesson(102);

        var ok = Assert.IsType<OkObjectResult>(response);
        var sessions = Assert.IsAssignableFrom<IEnumerable<SessionDto>>(ok.Value);
        Assert.Single(sessions);
    }

    [Fact]
    public async Task GetLessonsForScheduling_ReturnsOkWithRows()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.GetLessonsForSchedulingAsync())
            .ReturnsAsync(new List<LessonSchedulingRow>
            {
                new()
                {
                    LessonId = 1,
                    InstructorUserId = InstructorId,
                    Enrollment = 0,
                    MaxCapacity = 35,
                    TimesPerWeek = 2,
                    CourseCode = "X",
                    CourseTitle = "Y"
                }
            });

        var controller = new AdminController(
            lessonService.Object,
            new Mock<ICourseService>().Object,
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetLessonsForScheduling();

        var ok = Assert.IsType<OkObjectResult>(response);
        var rows = Assert.IsAssignableFrom<IEnumerable<LessonSchedulingRow>>(ok.Value);
        Assert.Single(rows);
    }
}

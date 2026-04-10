using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Web.Controllers;

namespace MyAdaAttendanceService.Web.Tests;

public class AdminControllerTests
{
    [Fact]
    public async Task GetAllLessons_ReturnsOkWithLessons()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.GetAllLessonsAsync())
            .ReturnsAsync(new List<LessonDto>
            {
                new() { Id = 1, Name = "Algorithms", Code = "CSE201", InstructorId = 11 }
            });

        var sessionService = new Mock<ISessionService>();
        var attendanceService = new Mock<IAttendanceService>();
        var adminAttendanceService = new Mock<IAdminAttendanceService>();
        var controller = new AdminController(
            lessonService.Object,
            sessionService.Object,
            attendanceService.Object,
            adminAttendanceService.Object);

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
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object);

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
            sessionService.Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object);

        var response = await controller.GetSessionsByLesson(102);

        var ok = Assert.IsType<OkObjectResult>(response);
        var sessions = Assert.IsAssignableFrom<IEnumerable<SessionDto>>(ok.Value);
        Assert.Single(sessions);
    }
}

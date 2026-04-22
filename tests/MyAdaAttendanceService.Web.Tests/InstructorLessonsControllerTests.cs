using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Web.Controllers;

namespace MyAdaAttendanceService.Web.Tests;

public class InstructorLessonsControllerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static InstructorLessonsController CreateController(
        ILessonService lessonService,
        ISessionService sessionService,
        Guid? userId = null)
    {
        var controller = new InstructorLessonsController(lessonService, sessionService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, (userId ?? UserId).ToString())
                }, "test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetMyLessons_ReturnsOk()
    {
        var lessonSvc = new Mock<ILessonService>();
        lessonSvc.Setup(x => x.GetMyLessonsAsync(UserId)).ReturnsAsync(new List<LessonDto>());
        var controller = CreateController(lessonSvc.Object, new Mock<ISessionService>().Object);

        var response = await controller.GetMyLessons(UserId);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task GetMyLessons_ReturnsOk_WhenRouteIdDoesNotMatchClaim_CurrentlyNoAuthEnforcement()
    {
        var lessonSvc = new Mock<ILessonService>();
        lessonSvc.Setup(x => x.GetMyLessonsAsync(OtherUserId)).ReturnsAsync(new List<LessonDto>());
        var controller = CreateController(lessonSvc.Object, new Mock<ISessionService>().Object, userId: UserId);

        var response = await controller.GetMyLessons(OtherUserId);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task GetMyLesson_ReturnsUnauthorized_WhenServiceThrows()
    {
        var lessonSvc = new Mock<ILessonService>();
        lessonSvc.Setup(x => x.GetMyLessonByIdAsync(UserId, 10)).ThrowsAsync(new UnauthorizedAccessException("no"));
        var controller = CreateController(lessonSvc.Object, new Mock<ISessionService>().Object);

        var response = await controller.GetMyLesson(UserId, 10);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task GetSessions_ReturnsOk()
    {
        var sessionSvc = new Mock<ISessionService>();
        sessionSvc.Setup(x => x.GetSessionsByLessonAsync(UserId, 10)).ReturnsAsync(new List<SessionDto>());
        var controller = CreateController(new Mock<ILessonService>().Object, sessionSvc.Object);

        var response = await controller.GetSessions(UserId, 10);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task CreateSession_ReturnsCreatedAtAction()
    {
        var sessionSvc = new Mock<ISessionService>();
        sessionSvc.Setup(x => x.CreateSessionAsync(UserId, 10, It.IsAny<CreateSessionDto>()))
            .ReturnsAsync(new SessionDto { Id = 5, LessonId = 10, LessonName = "Algorithms", StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) });

        var controller = CreateController(new Mock<ILessonService>().Object, sessionSvc.Object);
        var response = await controller.CreateSession(UserId, 10, new CreateSessionDto { StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) });

        Assert.IsType<CreatedAtActionResult>(response);
    }

    [Fact]
    public async Task DeleteSession_ReturnsNoContent_OnSuccess()
    {
        var sessionSvc = new Mock<ISessionService>();
        sessionSvc.Setup(x => x.DeleteSessionAsync(UserId, 5)).Returns(Task.CompletedTask);
        var controller = CreateController(new Mock<ILessonService>().Object, sessionSvc.Object);

        var response = await controller.DeleteSession(UserId, 10, 5);

        Assert.IsType<NoContentResult>(response);
    }
}

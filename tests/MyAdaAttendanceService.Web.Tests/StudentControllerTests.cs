using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Web.Controllers;

namespace MyAdaAttendanceService.Web.Tests;

public class StudentControllerTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static StudentController CreateController(
        IStudentAttendanceService studentAttendanceService,
        IAttendanceService attendanceService,
        Guid? userId = null)
    {
        var controller = new StudentController(studentAttendanceService, attendanceService);
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
    public async Task GetMyEnrollments_ReturnsOk()
    {
        var svc = new Mock<IStudentAttendanceService>();
        svc.Setup(x => x.GetMyLessonsAsync(UserId)).ReturnsAsync(new List<StudentLessonDto>());
        var controller = CreateController(svc.Object, new Mock<IAttendanceService>().Object);

        var response = await controller.GetMyEnrollments(UserId);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task GetMyAttendanceByLesson_ReturnsNotFound_WhenServiceThrows()
    {
        var svc = new Mock<IStudentAttendanceService>();
        svc.Setup(x => x.GetMyAttendanceByLessonAsync(UserId, 10)).ThrowsAsync(new KeyNotFoundException("missing"));
        var controller = CreateController(svc.Object, new Mock<IAttendanceService>().Object);

        var response = await controller.GetMyAttendanceByLesson(UserId, 10);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task ScanAttendance_ReturnsOk_WhenServiceAcceptsScan()
    {
        var attendanceSvc = new Mock<IAttendanceService>();
        attendanceSvc.Setup(x => x.MarkAttendanceByQrAsync(It.Is<QrScanRequestDto>(d => d.StudentId == UserId)))
            .ReturnsAsync(new QrScanResponseDto { Success = true });

        var controller = CreateController(new Mock<IStudentAttendanceService>().Object, attendanceSvc.Object);
        var response = await controller.ScanAttendance(UserId, new QrScanRequestDto { Token = "t" });

        Assert.IsType<OkObjectResult>(response);
    }
}

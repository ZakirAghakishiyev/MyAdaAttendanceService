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
    private static StudentController CreateController(
        IStudentAttendanceService studentAttendanceService,
        IAttendanceService attendanceService,
        int userId = 7)
    {
        var controller = new StudentController(studentAttendanceService, attendanceService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }, "test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetMyEnrollments_ReturnsOk()
    {
        var svc = new Mock<IStudentAttendanceService>();
        svc.Setup(x => x.GetMyLessonsAsync(7)).ReturnsAsync(new List<StudentLessonDto>());
        var controller = CreateController(svc.Object, new Mock<IAttendanceService>().Object);

        var response = await controller.GetMyEnrollments();

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task GetMyAttendanceByLesson_ReturnsNotFound_WhenServiceThrows()
    {
        var svc = new Mock<IStudentAttendanceService>();
        svc.Setup(x => x.GetMyAttendanceByLessonAsync(7, 10)).ThrowsAsync(new KeyNotFoundException("missing"));
        var controller = CreateController(svc.Object, new Mock<IAttendanceService>().Object);

        var response = await controller.GetMyAttendanceByLesson(10);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task ScanAttendance_ReturnsOk_WhenServiceAcceptsScan()
    {
        var attendanceSvc = new Mock<IAttendanceService>();
        attendanceSvc.Setup(x => x.MarkAttendanceByQrAsync(7, It.IsAny<QrScanRequestDto>()))
            .ReturnsAsync(new QrScanResponseDto { Success = true });

        var controller = CreateController(new Mock<IStudentAttendanceService>().Object, attendanceSvc.Object);
        var response = await controller.ScanAttendance(new QrScanRequestDto { Token = "t" });

        Assert.IsType<OkObjectResult>(response);
    }
}


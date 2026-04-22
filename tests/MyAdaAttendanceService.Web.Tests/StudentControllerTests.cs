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
    private static readonly Guid StudentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

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
                    new Claim(ClaimTypes.NameIdentifier, (userId ?? StudentId).ToString())
                }, "test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task ScanAttendance_ReturnsOk()
    {
        var studentSvc = new Mock<IStudentAttendanceService>();
        var attendanceSvc = new Mock<IAttendanceService>();
        attendanceSvc.Setup(x => x.MarkAttendanceByQrAsync(It.Is<QrScanRequestDto>(d => d.StudentId == StudentId)))
            .ReturnsAsync(new QrScanResponseDto { Success = true });

        var controller = CreateController(studentSvc.Object, attendanceSvc.Object);
        var response = await controller.ScanAttendance(StudentId, new QrScanRequestDto { Token = "t" });

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task ScanAttendance_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        var studentSvc = new Mock<IStudentAttendanceService>();
        var attendanceSvc = new Mock<IAttendanceService>();
        attendanceSvc.Setup(x => x.MarkAttendanceByQrAsync(It.Is<QrScanRequestDto>(d => d.StudentId == StudentId)))
            .ThrowsAsync(new ArgumentException("bad"));

        var controller = CreateController(studentSvc.Object, attendanceSvc.Object);
        var response = await controller.ScanAttendance(StudentId, new QrScanRequestDto { Token = "t" });

        Assert.IsType<BadRequestObjectResult>(response);
    }
}

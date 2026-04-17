using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Web.Controllers;

namespace MyAdaAttendanceService.Web.Tests;

public class AttendanceControllerTests
{
    private static readonly Guid StudentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static AttendanceController CreateController(IAttendanceService attendanceService, Guid? userId = null)
    {
        var controller = new AttendanceController(attendanceService);
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
    public async Task ScanQr_ReturnsOk()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.MarkAttendanceByQrAsync(It.Is<QrScanRequestDto>(d => d.StudentId == StudentId)))
            .ReturnsAsync(new QrScanResponseDto { Success = true });

        var controller = CreateController(svc.Object);
        var response = await controller.ScanQr(StudentId, new QrScanRequestDto { Token = "t" });

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task ScanQr_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.MarkAttendanceByQrAsync(It.Is<QrScanRequestDto>(d => d.StudentId == StudentId)))
            .ThrowsAsync(new ArgumentException("bad"));

        var controller = CreateController(svc.Object);
        var response = await controller.ScanQr(StudentId, new QrScanRequestDto { Token = "t" });

        Assert.IsType<BadRequestObjectResult>(response);
    }
}

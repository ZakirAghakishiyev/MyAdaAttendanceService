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
    private static AttendanceController CreateController(IAttendanceService attendanceService, int userId = 7)
    {
        var controller = new AttendanceController(attendanceService);
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
    public async Task ScanQr_ReturnsOk()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.MarkAttendanceByQrAsync(7, It.IsAny<QrScanRequestDto>()))
            .ReturnsAsync(new QrScanResponseDto { Success = true });

        var controller = CreateController(svc.Object);
        var response = await controller.ScanQr(new QrScanRequestDto { Token = "t" });

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task ScanQr_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.MarkAttendanceByQrAsync(7, It.IsAny<QrScanRequestDto>()))
            .ThrowsAsync(new ArgumentException("bad"));

        var controller = CreateController(svc.Object);
        var response = await controller.ScanQr(new QrScanRequestDto { Token = "t" });

        Assert.IsType<BadRequestObjectResult>(response);
    }
}


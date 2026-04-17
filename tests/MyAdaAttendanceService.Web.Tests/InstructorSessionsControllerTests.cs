using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Web.Controllers;

namespace MyAdaAttendanceService.Web.Tests;

public class InstructorSessionsControllerTests
{
    private static readonly Guid InstructorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StudentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static InstructorSessionsController CreateController(IAttendanceService attendanceService, Guid? userId = null)
    {
        var controller = new InstructorSessionsController(attendanceService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, (userId ?? InstructorId).ToString())
                }, "test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task ActivateAttendance_ReturnsOk_OnSuccess()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.ActivateAttendanceAsync(InstructorId, 5))
            .ReturnsAsync(new AttendanceActivationResultDto { SessionId = 5, IsAttendanceActive = true });

        var controller = CreateController(svc.Object);
        var response = await controller.ActivateAttendance(InstructorId, 5);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task ActivateAttendance_ReturnsUnauthorized_WhenServiceThrows()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.ActivateAttendanceAsync(InstructorId, 5))
            .ThrowsAsync(new UnauthorizedAccessException("nope"));

        var controller = CreateController(svc.Object);
        var response = await controller.ActivateAttendance(InstructorId, 5);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task DeactivateAttendance_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.DeactivateAttendanceAsync(InstructorId, 5))
            .ThrowsAsync(new ArgumentException("bad"));

        var controller = CreateController(svc.Object);
        var response = await controller.DeactivateAttendance(InstructorId, 5);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task IssueQrToken_ReturnsOk()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.IssueQrTokenAsync(InstructorId, 5))
            .ReturnsAsync(new QrTokenResponseDto { SessionId = 5, Token = "t" });

        var controller = CreateController(svc.Object);
        var response = await controller.IssueQrToken(InstructorId, 5);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task GetAttendance_ReturnsNotFound_WhenServiceThrowsKeyNotFound()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.GetSessionAttendanceAsync(InstructorId, 5))
            .ThrowsAsync(new KeyNotFoundException("missing"));

        var controller = CreateController(svc.Object);
        var response = await controller.GetAttendance(InstructorId, 5);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task GetAttendanceSummary_ReturnsOk()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.GetSessionAttendanceSummaryAsync(InstructorId, 5))
            .ReturnsAsync(new AttendanceSummaryDto { SessionId = 5, TotalStudents = 10 });

        var controller = CreateController(svc.Object);
        var response = await controller.GetAttendanceSummary(InstructorId, 5);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task UpdateAttendance_ReturnsOk()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.UpdateAttendanceAsync(InstructorId, 5, StudentId, It.IsAny<UpdateAttendanceDto>()))
            .ReturnsAsync(new AttendanceDto { SessionId = 5, StudentId = StudentId, Status = "Present" });

        var controller = CreateController(svc.Object);
        var response = await controller.UpdateAttendance(InstructorId, 5, StudentId, new UpdateAttendanceDto { Status = "Present" });

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task FinalizeAttendance_ReturnsNoContent_OnSuccess()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.FinalizeAttendanceAsync(InstructorId, 5)).Returns(Task.CompletedTask);

        var controller = CreateController(svc.Object);
        var response = await controller.FinalizeAttendance(InstructorId, 5);

        Assert.IsType<NoContentResult>(response);
    }

    [Fact]
    public async Task BulkMarkAbsent_ReturnsNoContent_OnSuccess()
    {
        var svc = new Mock<IAttendanceService>();
        svc.Setup(x => x.BulkMarkAbsentAsync(InstructorId, 5)).Returns(Task.CompletedTask);

        var controller = CreateController(svc.Object);
        var response = await controller.BulkMarkAbsent(InstructorId, 5);

        Assert.IsType<NoContentResult>(response);
    }
}

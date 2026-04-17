using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Tests;

public class AdminAttendanceServiceTests
{
    private static readonly Guid StudentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task FixAttendanceAsync_Throws_WhenStatusInvalid()
    {
        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new SessionAttendance { Id = 1, SessionId = 10, StudentId = StudentId });

        var sessionRepo = new Mock<ILessonSessionRepository>();
        var service = new AdminAttendanceService(attendanceRepo.Object, sessionRepo.Object);

        var dto = new AdminAttendanceCorrectionDto { Status = "not-a-status", Note = "x" };

        await Assert.ThrowsAsync<ArgumentException>(() => service.FixAttendanceAsync(1, dto));
    }

    [Fact]
    public async Task FixAttendanceAsync_UpdatesAttendanceAndReturnsDto()
    {
        var attendance = new SessionAttendance { Id = 1, SessionId = 10, StudentId = StudentId, Status = AttendanceStatus.Absent };

        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(attendance);
        attendanceRepo.Setup(x => x.UpdateAsync(attendance)).ReturnsAsync(attendance);

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new LessonSession { Id = 10, LessonId = 102 });

        var service = new AdminAttendanceService(attendanceRepo.Object, sessionRepo.Object);
        var dto = new AdminAttendanceCorrectionDto { Status = "Present", Note = "fixed" };

        var result = await service.FixAttendanceAsync(1, dto);

        Assert.Equal(102, result.LessonId);
        Assert.Equal("Present", result.Status);
        Assert.True(attendance.IsManuallyAdjusted);
        Assert.Equal("fixed", attendance.InstructorNote);
        attendanceRepo.Verify(x => x.UpdateAsync(attendance), Times.Once);
    }

    [Fact]
    public async Task FixAttendanceAsync_SetsIsManuallyAdjustedTrue()
    {
        var attendance = new SessionAttendance { Id = 1, SessionId = 10, StudentId = StudentId, IsManuallyAdjusted = false };
        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(attendance);
        attendanceRepo.Setup(x => x.UpdateAsync(attendance)).ReturnsAsync(attendance);

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new LessonSession { Id = 10, LessonId = 102 });

        var service = new AdminAttendanceService(attendanceRepo.Object, sessionRepo.Object);
        await service.FixAttendanceAsync(1, new AdminAttendanceCorrectionDto { Status = "Late", Note = null });

        Assert.True(attendance.IsManuallyAdjusted);
    }

    [Fact]
    public async Task DeleteAttendanceAsync_RemovesAttendance()
    {
        var attendance = new SessionAttendance { Id = 1 };
        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(attendance);
        attendanceRepo.Setup(x => x.RemoveAsync(attendance)).Returns(Task.CompletedTask);

        var service = new AdminAttendanceService(attendanceRepo.Object, new Mock<ILessonSessionRepository>().Object);

        await service.DeleteAttendanceAsync(1);

        attendanceRepo.Verify(x => x.RemoveAsync(attendance), Times.Once);
    }

    [Fact]
    public async Task DeleteAttendanceAsync_Throws_WhenAttendanceNotFound()
    {
        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetByIdAsync(999)).ThrowsAsync(new KeyNotFoundException("not found"));

        var service = new AdminAttendanceService(attendanceRepo.Object, new Mock<ILessonSessionRepository>().Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAttendanceAsync(999));
    }
}


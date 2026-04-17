using Moq;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Tests;

public class StudentAttendanceServiceTests
{
    private static readonly Guid StudentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task GetMyLessonsAsync_ComputesCountsFromAttendances()
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetStudentLessonsAsync(StudentId)).ReturnsAsync(new List<Lesson>
        {
            new()
            {
                Id = 10,
                AcademicYear = 2026,
                Semester = AcademicSemester.Spring,
                CRN = "20001",
                Course = new Course { Code = "CSE201", Department = "CS", Name = "Algorithms", Credits = 3, TimesPerWeek = 2 }
            }
        });

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonSession>
        {
            new() { Id = 1, LessonId = 10 },
            new() { Id = 2, LessonId = 10 }
        });

        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SessionAttendance, bool>>>(), null, null, false))
            .ReturnsAsync(new List<SessionAttendance>
            {
                new() { SessionId = 1, StudentId = StudentId, Status = AttendanceStatus.Present },
                new() { SessionId = 2, StudentId = StudentId, Status = AttendanceStatus.Late }
            });

        var service = new StudentAttendanceService(lessonRepo.Object, attendanceRepo.Object, sessionRepo.Object);
        var result = (await service.GetMyLessonsAsync(StudentId)).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].TotalSessions);
        Assert.Equal(1, result[0].PresentCount);
        Assert.Equal(1, result[0].LateCount);
    }

    [Fact]
    public async Task GetMyLessonsAsync_ReturnsEmpty_WhenNoLessons()
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetStudentLessonsAsync(StudentId)).ReturnsAsync(new List<Lesson>());

        var service = new StudentAttendanceService(
            lessonRepo.Object,
            new Mock<ISessionAttendanceRepository>().Object,
            new Mock<ILessonSessionRepository>().Object);

        var result = await service.GetMyLessonsAsync(StudentId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyAttendanceByLessonAsync_UsesLessonNameAndCode()
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(new Lesson
        {
            Id = 10,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "20001",
            Course = new Course { Code = "CSE201", Department = "CS", Name = "Algorithms", Credits = 3, TimesPerWeek = 2 }
        });

        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetStudentAttendanceAsync(StudentId, 10)).ReturnsAsync(new List<SessionAttendance>
        {
            new()
            {
                Id = 1,
                SessionId = 2,
                StudentId = StudentId,
                Status = AttendanceStatus.Present,
                Session = new LessonSession { Date = new DateOnly(2026, 4, 10), StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(10,0) }
            }
        });

        var service = new StudentAttendanceService(
            lessonRepo.Object,
            attendanceRepo.Object,
            new Mock<ILessonSessionRepository>().Object);

        var result = (await service.GetMyAttendanceByLessonAsync(StudentId, 10)).ToList();

        Assert.Single(result);
        Assert.Equal("Algorithms", result[0].LessonName);
        Assert.Equal("CSE201", result[0].LessonCode);
    }

    [Fact]
    public async Task GetMyAttendanceByLessonAsync_ReturnsEmpty_WhenNoRecords()
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(new Lesson
        {
            Id = 10,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "20001",
            Course = new Course { Code = "CSE201", Department = "CS", Name = "Algorithms", Credits = 3, TimesPerWeek = 2 }
        });

        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetStudentAttendanceAsync(StudentId, 10)).ReturnsAsync(new List<SessionAttendance>());

        var service = new StudentAttendanceService(
            lessonRepo.Object,
            attendanceRepo.Object,
            new Mock<ILessonSessionRepository>().Object);

        var result = await service.GetMyAttendanceByLessonAsync(StudentId, 10);

        Assert.Empty(result);
    }
}


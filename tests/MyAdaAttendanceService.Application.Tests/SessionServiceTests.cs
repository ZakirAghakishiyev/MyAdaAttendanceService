using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Tests;

public class SessionServiceTests
{
    private static readonly Guid InstructorA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InstructorB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StudentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static Course CourseNamed(string name) => new()
    {
        Name = name,
        Department = "CS",
        Code = "CSE201",
        Credits = 3,
        TimesPerWeek = 2
    };

    private static SessionService CreateService(
        Mock<ILessonSessionRepository> sessionRepo,
        Mock<ILessonRepository> lessonRepo,
        Mock<ISessionAttendanceRepository> attendanceRepo,
        Mock<ILessonEnrollmentRepository> enrollmentRepo)
        => new(sessionRepo.Object, lessonRepo.Object, attendanceRepo.Object, enrollmentRepo.Object);

    [Fact]
    public async Task GetSessionsByLessonAsync_ThrowsUnauthorized_WhenInstructorDoesNotOwnLesson()
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(new Lesson { Id = 10, InstructorId = InstructorB, Course = CourseNamed("X") });

        var service = CreateService(
            new Mock<ILessonSessionRepository>(),
            lessonRepo,
            new Mock<ISessionAttendanceRepository>(),
            new Mock<ILessonEnrollmentRepository>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetSessionsByLessonAsync(InstructorA, 10));
    }

    [Fact]
    public async Task GetSessionsByLessonAdminAsync_ReturnsSessionsMapped()
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(new Lesson { Id = 10, InstructorId = InstructorB, Course = CourseNamed("Algorithms") });

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonSession>
        {
            new() { Id = 1, LessonId = 10, Date = new DateOnly(2026, 4, 10), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) }
        });

        var enrollmentRepo = new Mock<ILessonEnrollmentRepository>();
        enrollmentRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment> { new() { LessonId = 10, StudentId = StudentId } });

        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SessionAttendance, bool>>>(), null, null, false))
            .ReturnsAsync(new List<SessionAttendance>());

        var service = CreateService(sessionRepo, lessonRepo, attendanceRepo, enrollmentRepo);
        var result = (await service.GetSessionsByLessonAdminAsync(10)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Algorithms", result[0].LessonName);
        Assert.Equal(1, result[0].TotalStudents);
    }

    [Fact]
    public async Task GetSessionByIdAsync_ThrowsNotFound_WhenSessionMissing()
    {
        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdWithLessonAsync(99)).ReturnsAsync((LessonSession?)null);

        var service = CreateService(
            sessionRepo,
            new Mock<ILessonRepository>(),
            new Mock<ISessionAttendanceRepository>(),
            new Mock<ILessonEnrollmentRepository>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetSessionByIdAsync(InstructorA, 99));
    }

    [Fact]
    public async Task GetSessionByIdAsync_ThrowsUnauthorized_WhenInstructorDoesNotOwnSession()
    {
        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdWithLessonAsync(99)).ReturnsAsync(new LessonSession
        {
            Id = 99,
            LessonId = 10,
            Lesson = new Lesson { Id = 10, InstructorId = InstructorB, Course = CourseNamed("X") },
            Date = new DateOnly(2026, 4, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        });

        var service = CreateService(
            sessionRepo,
            new Mock<ILessonRepository>(),
            new Mock<ISessionAttendanceRepository>(),
            new Mock<ILessonEnrollmentRepository>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetSessionByIdAsync(InstructorA, 99));
    }

    [Fact]
    public async Task CreateSessionAsync_ThrowsUnauthorized_WhenInstructorDoesNotOwnLesson()
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(new Lesson { Id = 10, InstructorId = InstructorB, Course = CourseNamed("Algorithms") });

        var service = CreateService(
            new Mock<ILessonSessionRepository>(),
            lessonRepo,
            new Mock<ISessionAttendanceRepository>(),
            new Mock<ILessonEnrollmentRepository>());

        var dto = new CreateSessionDto
        {
            StartTime = new DateTime(2026, 4, 10, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc),
            Topic = "Intro"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateSessionAsync(InstructorA, 10, dto));
    }

    [Fact]
    public async Task CreateSessionAsync_AddsSessionAndMapsTimes()
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(new Lesson { Id = 10, InstructorId = InstructorA, Course = CourseNamed("Algorithms") });

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.AddAsync(It.IsAny<LessonSession>()))
            .ReturnsAsync((LessonSession s) => s);

        var service = CreateService(
            sessionRepo,
            lessonRepo,
            new Mock<ISessionAttendanceRepository>(),
            new Mock<ILessonEnrollmentRepository>());

        var dto = new CreateSessionDto
        {
            StartTime = new DateTime(2026, 4, 10, 9, 30, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 4, 10, 10, 30, 0, DateTimeKind.Utc),
            Topic = "Intro"
        };

        var created = await service.CreateSessionAsync(InstructorA, 10, dto);

        sessionRepo.Verify(x => x.AddAsync(It.Is<LessonSession>(s =>
            s.LessonId == 10 &&
            s.Date == new DateOnly(2026, 4, 10) &&
            s.StartTime == new TimeOnly(9, 30) &&
            s.EndTime == new TimeOnly(10, 30) &&
            s.Topic == "Intro")), Times.Once);

        Assert.Equal("Algorithms", created.LessonName);
    }

    [Fact]
    public async Task UpdateSessionAsync_UpdatesFieldsAndPersists()
    {
        var session = new LessonSession
        {
            Id = 5,
            LessonId = 10,
            Lesson = new Lesson { Id = 10, InstructorId = InstructorA, Course = CourseNamed("Algorithms") },
            Date = new DateOnly(2026, 4, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Topic = "Old"
        };

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdWithLessonAsync(5)).ReturnsAsync(session);
        sessionRepo.Setup(x => x.UpdateAsync(It.IsAny<LessonSession>())).ReturnsAsync((LessonSession s) => s);

        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetBySessionIdAsync(5)).ReturnsAsync(new List<SessionAttendance>());

        var enrollmentRepo = new Mock<ILessonEnrollmentRepository>();
        enrollmentRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>());

        var service = CreateService(sessionRepo, new Mock<ILessonRepository>(), attendanceRepo, enrollmentRepo);

        var dto = new UpdateSessionDto
        {
            StartTime = new DateTime(2026, 4, 10, 11, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc),
            Topic = "New"
        };

        var updated = await service.UpdateSessionAsync(InstructorA, 5, dto);

        Assert.Equal("New", updated.Topic);
        sessionRepo.Verify(x => x.UpdateAsync(It.Is<LessonSession>(s => s.Topic == "New")), Times.Once);
    }

    [Fact]
    public async Task DeleteSessionAsync_RemovesSession()
    {
        var session = new LessonSession
        {
            Id = 5,
            LessonId = 10,
            Lesson = new Lesson { Id = 10, InstructorId = InstructorA, Course = CourseNamed("Algorithms") }
        };

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdWithLessonAsync(5)).ReturnsAsync(session);
        sessionRepo.Setup(x => x.RemoveAsync(session)).Returns(Task.CompletedTask);

        var service = CreateService(sessionRepo, new Mock<ILessonRepository>(), new Mock<ISessionAttendanceRepository>(), new Mock<ILessonEnrollmentRepository>());

        await service.DeleteSessionAsync(InstructorA, 5);

        sessionRepo.Verify(x => x.RemoveAsync(session), Times.Once);
    }

    [Fact]
    public async Task ActivateAttendanceAsync_Throws_WhenAlreadyActive()
    {
        var session = new LessonSession
        {
            Id = 5,
            LessonId = 10,
            Lesson = new Lesson { Id = 10, InstructorId = InstructorA, Course = CourseNamed("Algorithms") },
            IsAttendanceActive = true
        };
        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdWithLessonAsync(5)).ReturnsAsync(session);

        var service = CreateService(sessionRepo, new Mock<ILessonRepository>(), new Mock<ISessionAttendanceRepository>(), new Mock<ILessonEnrollmentRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivateAttendanceAsync(InstructorA, 5));
    }

    [Fact]
    public async Task DeactivateAttendanceAsync_Throws_WhenNotActive()
    {
        var session = new LessonSession
        {
            Id = 5,
            LessonId = 10,
            Lesson = new Lesson { Id = 10, InstructorId = InstructorA, Course = CourseNamed("Algorithms") },
            IsAttendanceActive = false
        };
        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdWithLessonAsync(5)).ReturnsAsync(session);

        var service = CreateService(sessionRepo, new Mock<ILessonRepository>(), new Mock<ISessionAttendanceRepository>(), new Mock<ILessonEnrollmentRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeactivateAttendanceAsync(InstructorA, 5));
    }

    [Fact]
    public async Task BulkGenerateSessionsAsync_CreatesOneSessionPerMatchingWeekday()
    {
        var lesson = new Lesson
        {
            Id = 10,
            InstructorId = InstructorA,
            Course = CourseNamed("Algorithms")
        };

        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(lesson);

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonSession>());
        sessionRepo
            .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<LessonSession>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<LessonSession>, CancellationToken>((sessions, _) =>
            {
                var id = 500;
                foreach (var s in sessions)
                    s.Id = id++;
            })
            .Returns(Task.CompletedTask);

        var enrollmentRepo = new Mock<ILessonEnrollmentRepository>();
        enrollmentRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>());

        var service = CreateService(sessionRepo, lessonRepo, new Mock<ISessionAttendanceRepository>(), enrollmentRepo);

        var dto = new BulkGenerateSessionsDto
        {
            FromDate = new DateOnly(2026, 4, 6),
            ToDate = new DateOnly(2026, 4, 13),
            WeeklySlots =
            [
                new WeeklySessionSlotDto
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0)
                }
            ],
            Topic = "Lecture"
        };

        var result = await service.BulkGenerateSessionsAsync(InstructorA, 10, dto);

        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(0, result.SkippedDuplicateCount);
        Assert.Equal(2, result.CreatedSessions.Count);
        sessionRepo.Verify(
            x => x.AddRangeAsync(It.Is<IEnumerable<LessonSession>>(e => e.Count() == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BulkGenerateSessionsAsync_SkipsExistingDateAndTime()
    {
        var lesson = new Lesson
        {
            Id = 10,
            InstructorId = InstructorA,
            Course = CourseNamed("Algorithms")
        };

        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(lesson);

        var existingMonday = new LessonSession
        {
            Id = 1,
            LessonId = 10,
            Date = new DateOnly(2026, 4, 6),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        };

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonSession> { existingMonday });
        sessionRepo
            .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<LessonSession>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<LessonSession>, CancellationToken>((sessions, _) =>
            {
                foreach (var s in sessions)
                    s.Id = 600;
            })
            .Returns(Task.CompletedTask);

        var enrollmentRepo = new Mock<ILessonEnrollmentRepository>();
        enrollmentRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>());

        var service = CreateService(sessionRepo, lessonRepo, new Mock<ISessionAttendanceRepository>(), enrollmentRepo);

        var dto = new BulkGenerateSessionsDto
        {
            FromDate = new DateOnly(2026, 4, 6),
            ToDate = new DateOnly(2026, 4, 13),
            WeeklySlots =
            [
                new WeeklySessionSlotDto
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0)
                }
            ]
        };

        var result = await service.BulkGenerateSessionsAsync(InstructorA, 10, dto);

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.SkippedDuplicateCount);
        sessionRepo.Verify(
            x => x.AddRangeAsync(It.Is<IEnumerable<LessonSession>>(e => e.Count() == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BulkGenerateSessionsAsync_ThrowsUnauthorized_WhenInstructorDoesNotOwnLesson()
    {
        var lesson = new Lesson
        {
            Id = 10,
            InstructorId = InstructorB,
            Course = CourseNamed("Algorithms")
        };

        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(lesson);

        var service = CreateService(
            new Mock<ILessonSessionRepository>(),
            lessonRepo,
            new Mock<ISessionAttendanceRepository>(),
            new Mock<ILessonEnrollmentRepository>());

        var dto = new BulkGenerateSessionsDto
        {
            FromDate = new DateOnly(2026, 4, 6),
            ToDate = new DateOnly(2026, 4, 6),
            WeeklySlots =
            [
                new WeeklySessionSlotDto
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0)
                }
            ]
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.BulkGenerateSessionsAsync(InstructorA, 10, dto));
    }

    [Fact]
    public async Task BulkGenerateSessionsAdminAsync_DoesNotRequireInstructorOwnership()
    {
        var lesson = new Lesson
        {
            Id = 10,
            InstructorId = InstructorB,
            Course = CourseNamed("Algorithms")
        };

        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdWithCourseAsync(10)).ReturnsAsync(lesson);

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonSession>());
        sessionRepo
            .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<LessonSession>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<LessonSession>, CancellationToken>((sessions, _) =>
            {
                foreach (var s in sessions)
                    s.Id = 700;
            })
            .Returns(Task.CompletedTask);

        var enrollmentRepo = new Mock<ILessonEnrollmentRepository>();
        enrollmentRepo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>());

        var service = CreateService(sessionRepo, lessonRepo, new Mock<ISessionAttendanceRepository>(), enrollmentRepo);

        var dto = new BulkGenerateSessionsDto
        {
            FromDate = new DateOnly(2026, 4, 6),
            ToDate = new DateOnly(2026, 4, 6),
            WeeklySlots =
            [
                new WeeklySessionSlotDto
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0)
                }
            ]
        };

        var result = await service.BulkGenerateSessionsAdminAsync(10, dto);

        Assert.Equal(1, result.CreatedCount);
        sessionRepo.Verify(
            x => x.AddRangeAsync(It.IsAny<IEnumerable<LessonSession>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}


using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonEnrollment> LessonEnrollments => Set<LessonEnrollment>();
    public DbSet<LessonSession> LessonSessions => Set<LessonSession>();
    public DbSet<SessionAttendance> SessionAttendances => Set<SessionAttendance>();
    public DbSet<AttendanceActivation> AttendanceActivations => Set<AttendanceActivation>();
    public DbSet<AttendanceScanLog> AttendanceScanLogs => Set<AttendanceScanLog>();
    public DbSet<LessonTime> LessonTimes => Set<LessonTime>();
    public DbSet<Timeslot> Timeslots => Set<Timeslot>();
}

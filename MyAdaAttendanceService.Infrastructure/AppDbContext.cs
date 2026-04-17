using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<ExternalUserDirectoryEntry> ExternalUserDirectoryEntries => Set<ExternalUserDirectoryEntry>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonEnrollment> LessonEnrollments => Set<LessonEnrollment>();
    public DbSet<LessonSession> LessonSessions => Set<LessonSession>();
    public DbSet<SessionAttendance> SessionAttendances => Set<SessionAttendance>();
    public DbSet<AttendanceActivation> AttendanceActivations => Set<AttendanceActivation>();
    public DbSet<AttendanceScanLog> AttendanceScanLogs => Set<AttendanceScanLog>();
    public DbSet<LessonTime> LessonTimes => Set<LessonTime>();
    public DbSet<Timeslot> Timeslots => Set<Timeslot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>()
            .HasIndex(c => new { c.Department, c.Code })
            .IsUnique();

        modelBuilder.Entity<ExternalUserDirectoryEntry>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<ExternalUserDirectoryEntry>()
            .HasIndex(x => new { x.Role, x.UserName });

        modelBuilder.Entity<Lesson>()
            .Property(e => e.MaxCapacity)
            .HasColumnName("Capacity");

        modelBuilder.Entity<Lesson>()
            .Property(e => e.Semester)
            .HasConversion<int>();

        modelBuilder.Entity<Lesson>()
            .HasIndex(l => new { l.AcademicYear, l.Semester, l.CRN })
            .IsUnique();

        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

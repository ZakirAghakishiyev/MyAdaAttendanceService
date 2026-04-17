using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Infrastructure;

namespace MyAdaAttendanceService.Web.Seeding;

public static class LessonPipeSeeder
{
    private static readonly Regex PrimaryInstructorRegex = new(
        @"^(.+?)\(Primary\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TermRegex = new(
        @"^(Fall|Spring|Summer)\s*(\d{4})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static async Task SeedIfEmptyAsync(
        AppDbContext db,
        IHostEnvironment env,
        IConfiguration config,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!config.GetValue("Database:SeedLessonsIfEmpty", false))
            return;

        if (await db.Lessons.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Lessons table already has rows; skipping pipe seed.");
            return;
        }

        var relative = config["Database:LessonsPipeRelativePath"] ?? Path.Combine("SeedData", "lessons.pipe.txt");
        var path = Path.Combine(env.ContentRootPath, relative);
        if (!File.Exists(path))
        {
            logger.LogWarning("Lesson seed file not found at {Path}. Copy database/seed/lessons.pipe.txt into the published SeedData folder.", path);
            return;
        }

        var termRaw = config["Database:SeedSemester"] ?? "Spring2026";
        var (academicYear, academicSemester) = ParseSeedTerm(termRaw);
        var roomId = config.GetValue("Database:SeedRoomId", 1);
        var credits = config.GetValue("Database:SeedDefaultCredits", 3);

        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        if (lines.Length < 2)
        {
            logger.LogWarning("Lesson seed file is empty or has no data rows.");
            return;
        }

        var rows = new List<PipeRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var parts = line.Split('|');
            if (parts.Length < 7)
            {
                logger.LogWarning("Skipping malformed line {LineNumber}: expected 7 pipe-separated fields.", i + 1);
                continue;
            }

            rows.Add(new PipeRow(
                parts[0].Trim(),
                parts[1].Trim(),
                parts[2].Trim(),
                parts[3].Trim(),
                parts[4].Trim(),
                int.TryParse(parts[5].Trim(), out var seats) ? seats : 0,
                int.TryParse(parts[6].Trim(), out var lpw) ? lpw : 0));
        }

        if (rows.Count == 0)
        {
            logger.LogWarning("No valid lesson rows parsed from seed file.");
            return;
        }

        var instructorKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in rows)
            instructorKeys.Add(GetPrimaryInstructorKey(r.Instructor));

        var sorted = instructorKeys.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var directoryEntries = await db.ExternalUserDirectoryEntries
            .Where(x => x.Role == "instructor")
            .ToListAsync(cancellationToken);
        var instructorIdByKey = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var name in sorted)
        {
            var match = directoryEntries.FirstOrDefault(x =>
                string.Equals($"{x.FirstName} {x.LastName}".Trim(), name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.UserName, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Email, name, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
                instructorIdByKey[name] = match.UserId;
        }

        if (instructorIdByKey.Count == 0)
        {
            logger.LogWarning("No instructor mappings found in synced auth users; skipping lesson seed.");
            return;
        }

        var courseRows = rows
            .GroupBy(r => (r.SubjectDescription, r.CourseNumber))
            .Select(g => g.First())
            .ToList();

        var courses = new List<Course>(courseRows.Count);
        foreach (var r in courseRows)
        {
            courses.Add(new Course
            {
                Name = r.CourseTitle,
                Department = r.SubjectDescription,
                Code = r.CourseNumber,
                Credits = credits,
                TimesPerWeek = r.LessonsPerWeek
            });
        }

        await db.Courses.AddRangeAsync(courses, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var courseIdByKey = courses.ToDictionary(c => (c.Department, c.Code), c => c.Id);

        var seq = 0;
        var lessons = new List<Lesson>(rows.Count);
        foreach (var r in rows)
        {
            var key = GetPrimaryInstructorKey(r.Instructor);
            if (!instructorIdByKey.TryGetValue(key, out var instructorId))
            {
                logger.LogWarning("Skipping lesson seed row for instructor {Instructor} because no synced auth user matched.", key);
                continue;
            }
            var courseId = courseIdByKey[(r.SubjectDescription, r.CourseNumber)];
            seq++;
            var crn = CrnFormatter.Format(academicSemester, seq);
            lessons.Add(new Lesson
            {
                InstructorId = instructorId,
                RoomId = roomId,
                CourseId = courseId,
                AcademicYear = academicYear,
                Semester = academicSemester,
                CRN = crn,
                MaxCapacity = r.AvailableSeats
            });
        }

        if (lessons.Count == 0)
        {
            logger.LogWarning("No lessons were seeded because none of the instructors matched synced auth users.");
            return;
        }

        await db.Lessons.AddRangeAsync(lessons, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {Count} lessons from pipe file ({Instructors} distinct instructor keys), term {Year} {Semester}.",
            lessons.Count,
            instructorIdByKey.Count,
            academicYear,
            academicSemester);
    }

    private static (int AcademicYear, AcademicSemester Semester) ParseSeedTerm(string raw)
    {
        var t = raw.Trim();
        var m = TermRegex.Match(t);
        if (m.Success)
        {
            var sem = m.Groups[1].Value.ToUpperInvariant() switch
            {
                "FALL" => AcademicSemester.Fall,
                "SPRING" => AcademicSemester.Spring,
                "SUMMER" => AcademicSemester.Summer,
                _ => AcademicSemester.Spring
            };
            var year = int.Parse(m.Groups[2].Value);
            return (year, sem);
        }

        return (DateTime.UtcNow.Year, AcademicSemester.Spring);
    }

    private static string GetPrimaryInstructorKey(string raw)
    {
        var t = raw.Trim();
        var m = PrimaryInstructorRegex.Match(t);
        if (m.Success)
            return m.Groups[1].Value.Trim();
        return t;
    }

    private sealed record PipeRow(
        string CourseTitle,
        string SubjectDescription,
        string CourseNumber,
        string Crn,
        string Instructor,
        int AvailableSeats,
        int LessonsPerWeek);
}

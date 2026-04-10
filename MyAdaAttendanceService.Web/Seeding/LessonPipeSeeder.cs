using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Infrastructure;

namespace MyAdaAttendanceService.Web.Seeding;

public static class LessonPipeSeeder
{
    private static readonly Regex PrimaryInstructorRegex = new(
        @"^(.+?)\(Primary\)",
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

        var semester = config["Database:SeedSemester"] ?? "Spring2026";
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
        var instructorIdByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        var next = 1;
        foreach (var name in sorted)
            instructorIdByKey[name] = next++;

        var lessons = new List<Lesson>(rows.Count);
        foreach (var r in rows)
        {
            var key = GetPrimaryInstructorKey(r.Instructor);
            lessons.Add(new Lesson
            {
                InstructorId = instructorIdByKey[key],
                RoomId = roomId,
                Semester = semester,
                CRN = r.Crn,
                Name = r.CourseTitle,
                Type = "Section",
                Department = r.SubjectDescription,
                Code = r.CourseNumber,
                Credits = credits,
                TimesPerWeek = r.LessonsPerWeek,
                Capacity = r.AvailableSeats
            });
        }

        await db.Lessons.AddRangeAsync(lessons, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {Count} lessons from pipe file ({Instructors} distinct instructor keys).",
            lessons.Count,
            instructorIdByKey.Count);
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

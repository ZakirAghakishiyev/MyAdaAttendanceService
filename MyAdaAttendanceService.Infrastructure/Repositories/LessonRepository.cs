using System.Data;
using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class LessonRepository : EfCoreRepository<Lesson>, ILessonRepository
{
    public LessonRepository(AppDbContext context) : base(context) { }

    public async Task<List<Lesson>> GetByInstructorIdAsync(Guid instructorId)
    {
        return await _dbSet
            .Include(x => x.Course)
            .Where(x => x.InstructorId == instructorId)
            .ToListAsync();
    }

    public async Task<Lesson?> GetByIdWithDetailsAsync(int lessonId)
    {
        return await _dbSet
            .Include(x => x.Course)
            .Include(x => x.Sessions)
            .Include(x => x.Enrollments)
            .FirstOrDefaultAsync(x => x.Id == lessonId);
    }

    public async Task<List<Lesson>> GetStudentLessonsAsync(Guid studentId)
    {
        return await _dbSet
            .Include(l => l.Course)
            .Where(l => l.Enrollments.Any(e => e.StudentId == studentId))
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid studentId, int lessonId)
    {
        return await _dbSet
            .AnyAsync(l => l.Id == lessonId && l.Enrollments.Any(e => e.StudentId == studentId));
    }

    public async Task<Lesson?> GetByIdWithCourseAsync(int lessonId)
    {
        return await _dbSet
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == lessonId);
    }

    public async Task<List<Lesson>> GetByCourseIdAsync(int courseId)
    {
        return await _dbSet
            .Include(l => l.Course)
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.Id)
            .ToListAsync();
    }

    public async Task<List<Lesson>> GetByAcademicTermAsync(int academicYear, AcademicSemester semester)
    {
        return await _dbSet
            .Include(l => l.Course)
            .Where(l => l.AcademicYear == academicYear && l.Semester == semester)
            .OrderBy(l => l.Id)
            .ToListAsync();
    }

    public async Task<Lesson> AddWithGeneratedCrnAsync(Lesson lesson)
    {
        if (!string.IsNullOrEmpty(lesson.CRN))
            throw new ArgumentException("CRN must be empty; it is assigned by the server.");

        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            lesson.CRN = await AllocateNextCrnAsync(lesson.AcademicYear, lesson.Semester);
            await _dbSet.AddAsync(lesson);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return lesson;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateLessonWithAutoCrnAsync(
        Lesson lesson,
        int newAcademicYear,
        AcademicSemester newSemester,
        int courseId,
        bool termChanged,
        Guid instructorId,
        int roomId,
        int maxCapacity)
    {
        if (!termChanged)
        {
            lesson.InstructorId = instructorId;
            lesson.RoomId = roomId;
            lesson.CourseId = courseId;
            lesson.AcademicYear = newAcademicYear;
            lesson.Semester = newSemester;
            lesson.MaxCapacity = maxCapacity;
            await UpdateAsync(lesson);
            return;
        }

        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            lesson.CRN = await AllocateNextCrnAsync(newAcademicYear, newSemester);
            lesson.InstructorId = instructorId;
            lesson.RoomId = roomId;
            lesson.CourseId = courseId;
            lesson.AcademicYear = newAcademicYear;
            lesson.Semester = newSemester;
            lesson.MaxCapacity = maxCapacity;
            _dbSet.Update(lesson);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<string> AllocateNextCrnAsync(int academicYear, AcademicSemester semester)
    {
        var prefix = CrnFormatter.PrefixChar(semester);
        var crns = await _dbSet
            .Where(l => l.AcademicYear == academicYear && l.Semester == semester)
            .Select(l => l.CRN)
            .ToListAsync();

        var maxSeq = 0;
        foreach (var crn in crns)
        {
            if (crn.Length == CrnFormatter.CrnLength && crn[0] == prefix
                && int.TryParse(crn.AsSpan(1, 4), out var n))
                maxSeq = Math.Max(maxSeq, n);
        }

        var next = maxSeq + 1;
        if (next > 9999)
            throw new InvalidOperationException("No available CRN numbers for this academic year and semester.");

        return CrnFormatter.Format(semester, next);
    }

    public async Task<IReadOnlyList<LessonSchedulingRow>> GetLessonSchedulingRowsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(l => l.Id)
            .Select(l => new LessonSchedulingRow
            {
                LessonId = l.Id,
                InstructorUserId = l.InstructorId,
                Enrollment = l.Enrollments.Count(),
                MaxCapacity = l.MaxCapacity,
                TimesPerWeek = l.Course!.TimesPerWeek,
                CourseCode = l.Course.Code,
                CourseTitle = l.Course.Name
            })
            .ToListAsync();
    }
}
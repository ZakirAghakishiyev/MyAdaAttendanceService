using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class LessonRepository : EfCoreRepository<Lesson>, ILessonRepository
{
    public LessonRepository(AppDbContext context) : base(context) { }

    public async Task<List<Lesson>> GetByInstructorIdAsync(int instructorId)
    {
        return await _dbSet
            .Where(x => x.InstructorId == instructorId)
            .ToListAsync();
    }

    public async Task<Lesson?> GetByIdWithDetailsAsync(int lessonId)
    {
        return await _dbSet
            .Include(x => x.Sessions)
            .Include(x => x.Enrollments)
            .FirstOrDefaultAsync(x => x.Id == lessonId);
    }

    public async Task<List<Lesson>> GetStudentLessonsAsync(int studentId)
    {
        return await _dbSet
            .Where(l => l.Enrollments.Any(e => e.StudentId == studentId))
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int studentId, int lessonId)
    {
        return await _dbSet
            .AnyAsync(l => l.Id == lessonId && l.Enrollments.Any(e => e.StudentId == studentId));
    }
}
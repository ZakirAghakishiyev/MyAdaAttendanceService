using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class LessonEnrollmentRepository : EfCoreRepository<LessonEnrollment>, ILessonEnrollmentRepository
{
    public LessonEnrollmentRepository(AppDbContext context) : base(context) { }

    public async Task<bool> ExistsAsync(int lessonId, int studentId)
    {
        return await _dbSet.AnyAsync(x =>
            x.LessonId == lessonId &&
            x.StudentId == studentId);
    }

    public async Task<List<LessonEnrollment>> GetByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Where(x => x.StudentId == studentId)
            .ToListAsync();
    }

    public async Task<List<LessonEnrollment>> GetByLessonIdAsync(int lessonId)
    {
        return await _dbSet
            .Where(x => x.LessonId == lessonId)
            .ToListAsync();
    }
}
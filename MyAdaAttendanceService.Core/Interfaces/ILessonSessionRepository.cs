using MyAdaAttendanceService.Core.Entities;
using System;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface ILessonSessionRepository : IRepository<LessonSession>
{
    Task<List<LessonSession>> GetByLessonIdAsync(int lessonId);

    Task<LessonSession?> GetByIdWithLessonAsync(int sessionId);

    Task<List<LessonSession>> GetUpcomingSessionsAsync(int lessonId, DateTime now);

    Task<List<LessonSession>> GetPastSessionsAsync(int lessonId, DateTime now);
}


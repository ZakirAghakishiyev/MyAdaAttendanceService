using MyAdaAttendanceService.Application.DTOs;

public interface IStudentAttendanceService
{
    Task<IEnumerable<StudentLessonDto>> GetMyLessonsAsync(Guid studentId);

    Task<IEnumerable<StudentAttendanceDto>> GetMyAttendanceByLessonAsync(Guid studentId, int lessonId);
}

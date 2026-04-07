using MyAdaAttendanceService.Application.DTOs;

public interface IStudentAttendanceService
{
    Task<IEnumerable<StudentLessonDto>> GetMyLessonsAsync(int studentId);

    Task<IEnumerable<StudentAttendanceDto>> GetMyAttendanceByLessonAsync(int studentId, int lessonId);
}

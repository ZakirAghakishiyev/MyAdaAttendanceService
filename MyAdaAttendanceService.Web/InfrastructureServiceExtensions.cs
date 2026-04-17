using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Interfaces;
using MyAdaAttendanceService.Infrastructure.Repositories;

namespace MyAdaAttendanceService.Web;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));

        services.AddHttpContextAccessor();
        services.AddTransient<AuthServiceBearerForwardingHandler>();
        services.AddHttpClient<IAuthUserClient, AuthUserClient>((sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["AuthService:BaseUrl"] ?? "http://51.20.193.29:5000/";
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<AuthServiceBearerForwardingHandler>();

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IExternalUserDirectoryService, ExternalUserDirectoryService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IStudentAttendanceService, StudentAttendanceService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IAdminAttendanceService, AdminAttendanceService>();

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IExternalUserDirectoryRepository, ExternalUserDirectoryRepository>();
        services.AddScoped<ILessonEnrollmentRepository, LessonEnrollmentRepository>();
        services.AddScoped<ILessonSessionRepository, LessonSessionRepository>();
        services.AddScoped<ISessionAttendanceRepository, SessionAttendanceRepository>();
        services.AddScoped<IAttendanceActivationRepository, AttendanceActivationRepository>();
        services.AddScoped<IAttendanceScanLogRepository, AttendanceScanLogRepository>();
        services.AddScoped<ILessonTimeRepository, LessonTimeRepository>();
        //services.AddScoped<ITimeslotRepository, TimeslotRepository>();
        return services;
    }
}

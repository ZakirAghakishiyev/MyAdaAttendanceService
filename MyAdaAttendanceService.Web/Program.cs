using AutoWrapper;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Infrastructure;
using MyAdaAttendanceService.Web.Seeding;
using Serilog;

namespace MyAdaAttendanceService.Web;
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Host.UseSerilog((_, loggerConfiguration) =>
        {
            loggerConfiguration
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 10,
                    shared: true);
        });

        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields =
                HttpLoggingFields.RequestMethod |
                HttpLoggingFields.RequestPath |
                HttpLoggingFields.ResponseStatusCode |
                HttpLoggingFields.Duration;
        });

        builder.Services.AddInfrastructure();
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        var app = builder.Build();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            await db.Database.MigrateAsync();
            await LessonPipeSeeder.SeedIfEmptyAsync(db, env, config, logger);
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseApiResponseAndExceptionWrapper(new AutoWrapperOptions
        {
            ShowApiVersion = false,
            ShowStatusCode = true,
            IsApiOnly = true,
            UseCustomSchema = false,
            EnableExceptionLogging = true
        });
        app.UseHttpLogging();
        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();
        app.Logger.LogInformation("MyAdaAttendanceService.Web started.");

        await app.RunAsync();
    }
}

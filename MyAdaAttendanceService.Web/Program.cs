using System.Text.Json.Serialization;
using AutoWrapper;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Infrastructure;
using MyAdaAttendanceService.Web.Seeding;
using Serilog;
using Microsoft.OpenApi.Models;

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
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        // Authentication/authorization is intentionally disabled.
        // We still forward incoming bearer tokens to downstream auth service calls.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a bearer token issued by the auth service."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

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
            if (config.GetValue("AuthService:SyncOnStartup", false))
            {
                var userDirectoryService = scope.ServiceProvider.GetRequiredService<IExternalUserDirectoryService>();
                await userDirectoryService.SyncUsersAsync();
            }
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
        app.UseCors();

        app.MapControllers();
        app.Logger.LogInformation("MyAdaAttendanceService.Web started.");

        await app.RunAsync();
    }

}

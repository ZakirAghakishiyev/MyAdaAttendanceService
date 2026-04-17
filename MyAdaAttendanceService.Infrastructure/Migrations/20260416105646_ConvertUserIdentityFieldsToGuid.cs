using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAdaAttendanceService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertUserIdentityFieldsToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old integer-based user ids were sample/local identities and cannot be meaningfully cast to auth GUIDs.
            // Clear dependent data, then convert columns with explicit USING clauses so PostgreSQL can apply the type change.
            migrationBuilder.Sql("""
                DELETE FROM "AttendanceScanLogs";
                DELETE FROM "SessionAttendances";
                DELETE FROM "AttendanceActivations";
                DELETE FROM "LessonEnrollments";
                DELETE FROM "LessonSessions";
                DELETE FROM "Lessons";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "SessionAttendances"
                ALTER COLUMN "UpdatedBy" TYPE uuid
                USING NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "SessionAttendances"
                ALTER COLUMN "StudentId" TYPE uuid
                USING gen_random_uuid();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Lessons"
                ALTER COLUMN "InstructorId" TYPE uuid
                USING gen_random_uuid();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "LessonEnrollments"
                ALTER COLUMN "StudentId" TYPE uuid
                USING gen_random_uuid();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AttendanceScanLogs"
                ALTER COLUMN "StudentId" TYPE uuid
                USING gen_random_uuid();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AttendanceActivations"
                ALTER COLUMN "CreatedByInstructorId" TYPE uuid
                USING NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SessionAttendances"
                ALTER COLUMN "UpdatedBy" TYPE integer
                USING NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "SessionAttendances"
                ALTER COLUMN "StudentId" TYPE integer
                USING 0;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Lessons"
                ALTER COLUMN "InstructorId" TYPE integer
                USING 0;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "LessonEnrollments"
                ALTER COLUMN "StudentId" TYPE integer
                USING 0;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AttendanceScanLogs"
                ALTER COLUMN "StudentId" TYPE integer
                USING 0;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AttendanceActivations"
                ALTER COLUMN "CreatedByInstructorId" TYPE integer
                USING NULL;
                """);
        }
    }
}

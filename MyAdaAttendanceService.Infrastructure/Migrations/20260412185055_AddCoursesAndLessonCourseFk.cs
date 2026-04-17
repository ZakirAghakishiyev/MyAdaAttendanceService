using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyAdaAttendanceService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursesAndLessonCourseFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Department = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Credits = table.Column<int>(type: "integer", nullable: false),
                    TimesPerWeek = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Department_Code",
                table: "Courses",
                columns: new[] { "Department", "Code" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Courses" ("Name", "Department", "Code", "Credits", "TimesPerWeek")
                SELECT DISTINCT ON ("Department", "Code")
                    "Name", "Department", "Code", "Credits", "TimesPerWeek"
                FROM "Lessons"
                ORDER BY "Department", "Code", "Id";
                """);

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "Lessons",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Lessons" AS l
                SET "CourseId" = c."Id"
                FROM "Courses" AS c
                WHERE l."Department" = c."Department" AND l."Code" = c."Code";
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "Lessons" ALTER COLUMN "CourseId" SET NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId",
                table: "Lessons",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Courses_CourseId",
                table: "Lessons",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Credits",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "TimesPerWeek",
                table: "Lessons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Courses_CourseId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_CourseId",
                table: "Lessons");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Lessons",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Credits",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Lessons",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TimesPerWeek",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "Lessons" AS l
                SET "Code" = c."Code",
                    "Credits" = c."Credits",
                    "Department" = c."Department",
                    "TimesPerWeek" = c."TimesPerWeek"
                FROM "Courses" AS c
                WHERE l."CourseId" = c."Id";
                """);

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Lessons");

            migrationBuilder.DropTable(
                name: "Courses");
        }
    }
}

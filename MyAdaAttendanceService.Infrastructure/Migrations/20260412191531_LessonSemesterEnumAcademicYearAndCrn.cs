using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAdaAttendanceService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LessonSemesterEnumAcademicYearAndCrn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicYear",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 2026);

            migrationBuilder.RenameColumn(
                name: "Semester",
                table: "Lessons",
                newName: "SemesterLegacy");

            migrationBuilder.AddColumn<int>(
                name: "Semester",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.Sql(
                """
                UPDATE "Lessons" SET "AcademicYear" = COALESCE(
                    NULLIF(SUBSTRING("SemesterLegacy" FROM '[0-9]{4}'), '')::integer,
                    2026);
                UPDATE "Lessons" SET "Semester" = CASE
                    WHEN "SemesterLegacy" ILIKE '%Fall%' THEN 1
                    WHEN "SemesterLegacy" ILIKE '%Summer%' THEN 3
                    ELSE 2
                END;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Lessons" l
                SET "CRN" = (
                    CASE l."Semester"
                        WHEN 1 THEN '1'
                        WHEN 2 THEN '2'
                        WHEN 3 THEN '3'
                    END
                ) || LPAD(r.rn::text, 4, '0')
                FROM (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "AcademicYear", "Semester" ORDER BY "Id") AS rn
                    FROM "Lessons"
                ) r
                WHERE l."Id" = r."Id";
                """);

            migrationBuilder.DropColumn(
                name: "SemesterLegacy",
                table: "Lessons");

            migrationBuilder.AlterColumn<string>(
                name: "CRN",
                table: "Lessons",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.Sql(
                """
                ALTER TABLE "Lessons" ALTER COLUMN "AcademicYear" DROP DEFAULT;
                ALTER TABLE "Lessons" ALTER COLUMN "Semester" DROP DEFAULT;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_AcademicYear_Semester_CRN",
                table: "Lessons",
                columns: new[] { "AcademicYear", "Semester", "CRN" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_AcademicYear_Semester_CRN",
                table: "Lessons");

            migrationBuilder.AlterColumn<string>(
                name: "CRN",
                table: "Lessons",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5);

            migrationBuilder.AddColumn<string>(
                name: "SemesterLegacy",
                table: "Lessons",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Lessons" SET "SemesterLegacy" =
                    CASE "Semester"
                        WHEN 1 THEN 'Fall'
                        WHEN 2 THEN 'Spring'
                        WHEN 3 THEN 'Summer'
                        ELSE 'Spring'
                    END || "AcademicYear"::text;
                """);

            migrationBuilder.DropColumn(
                name: "Semester",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "Lessons");

            migrationBuilder.RenameColumn(
                name: "SemesterLegacy",
                table: "Lessons",
                newName: "Semester");
        }
    }
}

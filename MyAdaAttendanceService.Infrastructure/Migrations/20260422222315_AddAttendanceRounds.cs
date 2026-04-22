using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAdaAttendanceService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Round",
                table: "AttendanceScanLogs",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Round",
                table: "AttendanceActivations",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.Sql(
                """
                UPDATE "AttendanceScanLogs" SET "Round" = 1
                WHERE "Accepted" = true AND "Round" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Round",
                table: "AttendanceScanLogs");

            migrationBuilder.DropColumn(
                name: "Round",
                table: "AttendanceActivations");
        }
    }
}

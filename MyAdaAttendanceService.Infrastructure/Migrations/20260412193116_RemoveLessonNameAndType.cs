using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAdaAttendanceService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLessonNameAndType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Lessons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Lessons",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Lessons",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}

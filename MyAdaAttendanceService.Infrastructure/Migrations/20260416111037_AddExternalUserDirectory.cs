using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyAdaAttendanceService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalUserDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalUserDirectoryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalUserDirectoryEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalUserDirectoryEntries_Role_UserName",
                table: "ExternalUserDirectoryEntries",
                columns: new[] { "Role", "UserName" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalUserDirectoryEntries_UserId",
                table: "ExternalUserDirectoryEntries",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalUserDirectoryEntries");
        }
    }
}

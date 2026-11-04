using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationReads",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationReads", x => new { x.NotificationId, x.EmployeeId });
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DepartmentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DepartmentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ItemTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ActorName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AgentScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DepartmentId",
                table: "Notifications",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationReads");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvidenceSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartmentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AuditItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    StoredPath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    UploaderEmployeeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UploaderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AgentScore = table.Column<int>(type: "INTEGER", nullable: true),
                    AgentDefectRate = table.Column<int>(type: "INTEGER", nullable: true),
                    AgentOutcome = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AgentFindings = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    AgentCheckedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProvidedEvidence = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    FinalOutcome = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FinalNote = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ReviewerEmployeeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ReviewerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenceSubmissions_AuditItems_AuditItemId",
                        column: x => x.AuditItemId,
                        principalTable: "AuditItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvidenceSubmissions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceSubmissions_AgentOutcome_FinalOutcome",
                table: "EvidenceSubmissions",
                columns: new[] { "AgentOutcome", "FinalOutcome" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceSubmissions_AuditItemId",
                table: "EvidenceSubmissions",
                column: "AuditItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceSubmissions_DepartmentId_AuditItemId",
                table: "EvidenceSubmissions",
                columns: new[] { "DepartmentId", "AuditItemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvidenceSubmissions");
        }
    }
}

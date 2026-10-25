using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<string>(type: "TEXT", nullable: false),
                    DepartmentName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "AuditItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartmentId = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    QuestionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    EvidencesJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditItems_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "DepartmentId", "DepartmentName" },
                values: new object[] { "library", "دائرة المكتبة العامة" });

            migrationBuilder.InsertData(
                table: "AuditItems",
                columns: new[] { "Id", "Category", "DepartmentId", "EvidencesJson", "QuestionsJson", "Status", "Title" },
                values: new object[,]
                {
                    { 1, "بنود مشتركة", "library", "نسخة الهيكل المعتمد||قرار الاعتماد||تعميم داخلي", "هل يوجد هيكل تنظيمي معتمد ومحدث؟||هل يوضح خطوط السلطة والمسؤوليات؟", "بانتظار الرفع", "الهيكل التنظيمي" },
                    { 10, "خاص بالمكتبة", "library", "نظام المكتبة||تقارير الاستخدام", "هل يوجد نظام مكتبة فعال؟||هل يتم تحديثه وصيانته؟", "بانتظار الرفع", "نظام إدارة المكتبة" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditItems_DepartmentId",
                table: "AuditItems",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditItems");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}

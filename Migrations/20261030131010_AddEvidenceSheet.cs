using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceSheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evidence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartmentName = table.Column<string>(type: "TEXT", nullable: false),
                    ItemTitle = table.Column<string>(type: "TEXT", nullable: false),
                    InstructionsEvidence = table.Column<string>(type: "TEXT", nullable: false),
                    InternalAuditEvidence = table.Column<string>(type: "TEXT", nullable: false),
                    InstructionsWorkflow = table.Column<string>(type: "TEXT", nullable: false),
                    InstructionsQuestions = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidence", x => x.Id);
                });

            // نسخة منصوصة من كل بند إلى الورقة المستقلة: الجهة وعنوان البند نصّاً القراءة لا وصلاً،
            // وعمودا الأدلة يتفرقان بمنسوب النصّ لا بعلاقة جدول. النصوص السبعة هي ما نُقل سابقاً
            // حرفياً من عمود «الأدلة المطلوبة» في كتاب تعليمات التدقيق الداخلي (البنود المشتركة).
            var bookEvidence =
                "'قرار الاعتماد', 'سجل التحديثات', 'سجل التحديث', 'سجل الإصدارات', " +
                "'طلبات تعديل الوثائق', 'نظام الأرشفة', 'تقارير تحديث الاشتراكات'";

            migrationBuilder.Sql($$"""
                INSERT INTO Evidence (DepartmentName, ItemTitle, InstructionsEvidence,
                                      InternalAuditEvidence, InstructionsWorkflow, InstructionsQuestions)
                SELECT COALESCE(d.DepartmentName, i.DepartmentId), i.Title,
                       COALESCE((SELECT group_concat(r.Text, char(10)) FROM EvidenceRequirements r
                                 WHERE r.AuditItemId = i.Id AND r.Text IN ({{bookEvidence}})), ''),
                       COALESCE((SELECT group_concat(r.Text, char(10)) FROM EvidenceRequirements r
                                 WHERE r.AuditItemId = i.Id AND r.Text NOT IN ({{bookEvidence}})), ''),
                       COALESCE(i.Workflow, ''),
                       COALESCE((SELECT group_concat(q.Text, char(10)) FROM Questions q
                                 WHERE q.AuditItemId = i.Id), '')
                FROM AuditItems i
                LEFT JOIN Departments d ON d.DepartmentId = i.DepartmentId
                ORDER BY i.DepartmentId, i.SortOrder;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Evidence");
        }
    }
}

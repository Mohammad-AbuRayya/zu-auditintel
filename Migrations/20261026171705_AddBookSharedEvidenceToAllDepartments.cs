using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBookSharedEvidenceToAllDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 915502,
                column: "Text",
                value: "سجلات استخدام منصة التعلم الإلكتروني وإحصائيات التفاعل للفصل الحالي");

            migrationBuilder.InsertData(
                table: "EvidenceRequirements",
                columns: new[] { "Id", "AuditItemId", "SortOrder", "Text" },
                values: new object[,]
                {
                    { 100504, 100, 4, "قرار الاعتماد" },
                    { 100505, 100, 5, "سجل التحديثات" },
                    { 101504, 101, 4, "سجل التحديث" },
                    { 104503, 104, 3, "سجل الإصدارات" },
                    { 104504, 104, 4, "طلبات تعديل الوثائق" },
                    { 104505, 104, 5, "نظام الأرشفة" },
                    { 200504, 200, 4, "قرار الاعتماد" },
                    { 200505, 200, 5, "سجل التحديثات" },
                    { 201503, 201, 3, "سجل التحديث" },
                    { 204503, 204, 3, "سجل الإصدارات" },
                    { 204504, 204, 4, "طلبات تعديل الوثائق" },
                    { 300504, 300, 4, "قرار الاعتماد" },
                    { 300505, 300, 5, "سجل التحديثات" },
                    { 301503, 301, 3, "سجل التحديث" },
                    { 305504, 305, 4, "سجل الإصدارات" },
                    { 305505, 305, 5, "طلبات تعديل الوثائق" },
                    { 305506, 305, 6, "نظام الأرشفة" },
                    { 406503, 406, 3, "سجل الإصدارات" },
                    { 406504, 406, 4, "طلبات تعديل الوثائق" },
                    { 406505, 406, 5, "نظام الأرشفة" },
                    { 500504, 500, 4, "قرار الاعتماد" },
                    { 500505, 500, 5, "سجل التحديثات" },
                    { 501503, 501, 3, "سجل التحديث" },
                    { 504503, 504, 3, "سجل الإصدارات" },
                    { 504504, 504, 4, "طلبات تعديل الوثائق" },
                    { 600504, 600, 4, "قرار الاعتماد" },
                    { 600505, 600, 5, "سجل التحديثات" },
                    { 601503, 601, 3, "سجل التحديث" },
                    { 602503, 602, 3, "تقارير امتثال تطبيق سياسات وتعليمات مركز الحاسوب" },
                    { 604503, 604, 3, "سجل الإصدارات" },
                    { 604504, 604, 4, "طلبات تعديل الوثائق" },
                    { 604505, 604, 5, "نظام الأرشفة" },
                    { 700504, 700, 4, "قرار الاعتماد" },
                    { 700505, 700, 5, "سجل التحديثات" },
                    { 701503, 701, 3, "سجل التحديث" },
                    { 703504, 703, 4, "سجل الإصدارات" },
                    { 703505, 703, 5, "طلبات تعديل الوثائق" },
                    { 703506, 703, 6, "نظام الأرشفة" },
                    { 800504, 800, 4, "قرار الاعتماد" },
                    { 800505, 800, 5, "سجل التحديثات" },
                    { 801503, 801, 3, "سجل التحديث" },
                    { 804503, 804, 3, "سجل الإصدارات" },
                    { 804504, 804, 4, "طلبات تعديل الوثائق" },
                    { 900504, 900, 4, "قرار الاعتماد" },
                    { 900505, 900, 5, "سجل التحديثات" },
                    { 901503, 901, 3, "سجل التحديث" },
                    { 904503, 904, 3, "سجل الإصدارات" },
                    { 904504, 904, 4, "طلبات تعديل الوثائق" },
                    { 904505, 904, 5, "نظام الأرشفة" },
                    { 915503, 915, 3, "عينة (3 مساقات) توضح رفع خطة المساق، المادة العلمية، الأنشطة والواجبات، والتغذية الراجعة للطلبة" },
                    { 1000504, 1000, 4, "قرار الاعتماد" },
                    { 1000505, 1000, 5, "سجل التحديثات" },
                    { 1001503, 1001, 3, "سجل التحديث" },
                    { 1100504, 1100, 4, "قرار الاعتماد" },
                    { 1100505, 1100, 5, "سجل التحديثات" },
                    { 1101503, 1101, 3, "سجل التحديث" },
                    { 1105503, 1105, 3, "سجل الإصدارات" },
                    { 1105504, 1105, 4, "طلبات تعديل الوثائق" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 100504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 100505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 101504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 104503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 104504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 104505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 200504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 200505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 201503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 204503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 204504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 300504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 300505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 301503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 305504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 305505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 305506);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 406503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 406504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 406505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 500504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 500505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 501503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 504503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 504504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 600504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 600505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 601503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 602503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 604503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 604504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 604505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 700504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 700505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 701503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 703504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 703505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 703506);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 800504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 800505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 801503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 804503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 804504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 900504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 900505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 901503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 904503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 904504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 904505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 915503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1000504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1000505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1001503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1100504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1100505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1101503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1105503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1105504);

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 915502,
                column: "Text",
                value: "عينة (3 مساقات) توضح رفع خطة المساق، المادة العلمية، الأنشطة والواجبات، والتغذية الراجعة للطلبة");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryEvidenceFromInstructionsBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1502,
                column: "Text",
                value: "قرار الاعتماد");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1503,
                column: "Text",
                value: "سجل التحديثات");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 2502,
                column: "Text",
                value: "سجل التحديث");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 4502,
                column: "Text",
                value: "سجل الإصدارات");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 15502,
                column: "Text",
                value: "تقارير تحديث الاشتراكات وقواعد البيانات");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 15503,
                column: "Text",
                value: "شواهد الورش التدريبية والأدلة الإرشادية لاستخدام قواعد البيانات");

            migrationBuilder.InsertData(
                table: "EvidenceRequirements",
                columns: new[] { "Id", "AuditItemId", "SortOrder", "Text" },
                values: new object[,]
                {
                    { 1504, 1, 4, "دليل تعميمه وإعلانه للموظفين" },
                    { 1505, 1, 5, "كشف بأسماء موظفي المكتبة ومسمياتهم وشعبهم الحالية" },
                    { 2503, 2, 3, "عينة (3 موظفين) من نماذج استلام الوصف الوظيفي الموقعة والمؤرخة" },
                    { 4503, 4, 3, "طلبات تعديل الوثائق" },
                    { 4504, 4, 4, "نظام الأرشفة" },
                    { 4505, 4, 5, "شواهد ضبط وتحديث فهارس المقتنيات (الكتب، الرسائل الجامعية، الدوريات) وسجلات التزويد والجرد" },
                    { 15504, 15, 4, "تقارير إحصائيات الدخول والتحميل الفعلية من قواعد البيانات الإلكترونية" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 2503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 4503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 4504);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 4505);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 15504);

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1502,
                column: "Text",
                value: "دليل تعميمه وإعلانه للموظفين");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1503,
                column: "Text",
                value: "كشف بأسماء موظفي المكتبة ومسمياتهم وشعبهم الحالية");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 2502,
                column: "Text",
                value: "عينة (3 موظفين) من نماذج استلام الوصف الوظيفي الموقعة والمؤرخة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 4502,
                column: "Text",
                value: "شواهد ضبط وتحديث فهارس المقتنيات (الكتب، الرسائل الجامعية، الدوريات) وسجلات التزويد والجرد");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 15502,
                column: "Text",
                value: "شواهد الورش التدريبية والأدلة الإرشادية لاستخدام قواعد البيانات");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 15503,
                column: "Text",
                value: "تقارير إحصائيات الدخول والتحميل الفعلية من قواعد البيانات الإلكترونية");
        }
    }
}

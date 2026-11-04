using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjectiveToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adjective",
                table: "Users",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            // الصفة مشتقة من المسمى لا من الدور: كل «مسؤول كلية» من الكادر الأكاديمي وبقية الحسابات موظفون.
            // وجهان للمعريف في التسميات القائمة — «مسؤول كلية الشريعة» و«مسؤول الكلية التقنية» — فكلاهما يدخل.
            migrationBuilder.Sql("""
                UPDATE Users
                   SET Adjective = CASE
                        WHEN JobTitle LIKE 'مسؤول كلية%' OR JobTitle LIKE 'مسؤول الكلية%'
                             THEN 'AcademicStaff'
                        ELSE 'Employee'
                   END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adjective",
                table: "Users");
        }
    }
}

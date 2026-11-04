using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTitleToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Users",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // القاعدات القديمة تحمل المسمى في FullName: ينتقل كما هو إلى JobTitle ثم يحلّ محله الاسم الشخصي.
            migrationBuilder.Sql("UPDATE Users SET JobTitle = FullName");

            // WHERE FullName = JobTitle يستثني أي اسم عدّله صاحبه في القاعدة، وELSE FullName يُبقي
            // من لا فرع له في القائمة — كحساب حُذف من الزراعة وبقي صفّه — على قيمته بلا فراغ يُخزن.
            migrationBuilder.Sql("""
                UPDATE Users
                   SET FullName = CASE EmployeeId
                        WHEN '1000101' THEN 'عمر سليمان دعيج المجالي'
                        WHEN '1000102' THEN 'ياسر عبد الفتاح صالح السلايمة'
                        WHEN '1000103' THEN 'ماهر سلامة سالم الحديد'
                        WHEN '1000104' THEN 'طارق محمد علي الخصاونة'
                        WHEN '1000105' THEN 'سعيد داود إبراهيم الشرع'
                        WHEN '1000106' THEN 'فيصل كامل يوسف المراعية'
                        WHEN '1000107' THEN 'بلال أحمد سالم الزريقات'
                        WHEN '1000108' THEN 'جواد محمود علي الطرمان'
                        WHEN '1000109' THEN 'رمضان عوض الله الحراحشة'
                        WHEN '1000110' THEN 'أنس عبد الرحمن نصير المدادحة'
                        WHEN '1000111' THEN 'يوسف عبد الله فؤاد القضاة'
                        WHEN '1000112' THEN 'حمزة اسماعيل محمد البطوش'
                        WHEN '1000113' THEN 'خالد ماجد سالم العبيدات'
                        WHEN '1000114' THEN 'رائد فهمي حسن الشقور'
                        WHEN '1000115' THEN 'محمود عبد اللطيف سليم الدبابنة'
                        WHEN '1000116' THEN 'زياد كمال الدين يوسف النعيمات'
                        WHEN '1000117' THEN 'ليث جمال عبد الرزاق الفناطسة'
                        WHEN '1000118' THEN 'صهيب نايف عقيل الزعبي'
                        WHEN '1000119' THEN 'رامي مالك فتحي الجازي'
                        WHEN '1000120' THEN 'سامر عبد الإله كريم العبادي'
                        WHEN '1000121' THEN 'وسيم عطية الله حمد المكرادي'
                        WHEN '1000122' THEN 'نور الدين حسني جميل الخرابشة'
                        WHEN '1000123' THEN 'أمير عبد الحكم نبيل الرمامنة'
                        WHEN '1000124' THEN 'عصام محمد رفيق الصمادي'
                        WHEN '1000125' THEN 'شادي محي الدين قاسم الزير'
                        WHEN '1000126' THEN 'عماد عبد الصمد كامل القرالة'
                        WHEN '1000127' THEN 'بدر محمد عبد الباسط الرواضية'
                        WHEN '3000301' THEN 'علي أحمد حسين الجبور'
                        WHEN '9999999' THEN 'محمد خالد عمر أبو رية'
                        ELSE FullName
                   END
                 WHERE FullName = JobTitle
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // رجوعاً: الاسم الشخصي الذي وضعته هذه الترحيلة يعود ليترك مكان المسمى، ثم يُسحب العمود.
            migrationBuilder.Sql("""
                UPDATE Users
                   SET FullName = JobTitle
                 WHERE FullName IN ('عمر سليمان دعيج المجالي', 'ياسر عبد الفتاح صالح السلايمة',
                        'ماهر سلامة سالم الحديد', 'طارق محمد علي الخصاونة', 'سعيد داود إبراهيم الشرع',
                        'فيصل كامل يوسف المراعية', 'بلال أحمد سالم الزريقات', 'جواد محمود علي الطرمان',
                        'رمضان عوض الله الحراحشة', 'أنس عبد الرحمن نصير المدادحة', 'يوسف عبد الله فؤاد القضاة',
                        'حمزة اسماعيل محمد البطوش', 'خالد ماجد سالم العبيدات', 'رائد فهمي حسن الشقور',
                        'محمود عبد اللطيف سليم الدبابنة', 'زياد كمال الدين يوسف النعيمات',
                        'ليث جمال عبد الرزاق الفناطسة', 'صهيب نايف عقيل الزعبي', 'رامي مالك فتحي الجازي',
                        'سامر عبد الإله كريم العبادي', 'وسيم عطية الله حمد المكرادي',
                        'نور الدين حسني جميل الخرابشة', 'أمير عبد الحكم نبيل الرمامنة',
                        'عصام محمد رفيق الصمادي', 'شادي محي الدين قاسم الزير', 'عماد عبد الصمد كامل القرالة',
                        'بدر محمد عبد الباسط الرواضية', 'علي أحمد حسين الجبور', 'محمد خالد عمر أبو رية')
                """);

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Users");
        }
    }
}

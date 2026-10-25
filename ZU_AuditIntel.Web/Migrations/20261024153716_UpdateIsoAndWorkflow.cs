using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIsoAndWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "IsoReference",
                table: "AuditItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkflowJson",
                table: "AuditItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "EvidencesJson", "IsoReference", "QuestionsJson", "WorkflowJson" },
                values: new object[] { "خاص بالمكتبة", "نسخة الهيكل التنظيمي المعتمد والموقع||دليل تعميمه وإعلانه للموظفين||كشف بأسماء موظفي المكتبة ومسمياتهم", "5.3", "", "التحقق من وجود هيكل تنظيمي معتمد للمكتبة العامة يوضح الشعب والأقسام وخطوط الإشراف ومطابقته للكادر الحالي." });

            migrationBuilder.InsertData(
                table: "AuditItems",
                columns: new[] { "Id", "Category", "DepartmentId", "EvidencesJson", "IsoReference", "QuestionsJson", "Status", "Title", "WorkflowJson" },
                values: new object[,]
                {
                    { 2, "خاص بالمكتبة", "library", "بطاقات الوصف الوظيفي المعتمدة لكافة وظائف المكتبة||عينة 3 موظفين من نماذج استلام الوصف الوظيفي الموقعة", "7.2", "", "بانتظار الرفع", "الوصف الوظيفي", "التحقق من وجود بطاقات وصف وظيفي معتمدة لجميع المسميات الوظيفية في المكتبة وتوقيع الموظفين عليها." },
                    { 3, "خاص بالمكتبة", "library", "وثيقة سياسة وتعليمات المكتبة العامة المعتمدة||الدليل الإرشادي لمستخدمي المكتبة||شواهد نشر السياسة على موقع الجامعة", "5.2 / 7.1.6", "", "بانتظار الرفع", "سياسة المكتبة", "التحقق من وجود سياسة وتعليمات واضحة ومعتمدة للمكتبة العامة تنظم التزويد والإعارة واستخدام مرافق ومصادر المكتبة." },
                    { 4, "خاص بالمكتبة", "library", "القائمة الرئيسية للوثائق والسجلات المعتمدة (Master List)||شواهد ضبط وتحديث فهارس المقتنيات وسجلات التزويد والجرد", "7.1.6 / 7.5", "", "بانتظار الرفع", "ضبط الفهارس والسجلات", "التحقق من ضبط فهارس المكتبة (الورقية / الإلكترونية) وكافة السجلات والنماذج المعتمدة وفق متطلبات نظام الجودة." },
                    { 5, "خاص بالمكتبة", "library", "تقارير فنية وإحصائية مستخرجة من نظام إدارة المكتبة الآلي||شواهد عمل بوابة البحث الآلي OPAC||آلية ضبط صلاحيات النظام والنسخ الاحتياطي", "7.1.3 / 7.1.6", "", "بانتظار الرفع", "نظام إدارة المكتبة", "التحقق من كفاءة النظام الآلي لإدارة المكتبة في الفهرسة والبحث والإعارة وإصدار التقارير الإحصائية الدقيقة." }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "DepartmentId", "DepartmentName" },
                values: new object[,]
                {
                    { "admission", "دائرة القبول والتسجيل" },
                    { "faculties", "الكليات الأكاديمية" },
                    { "health_center", "المركز الصحي" },
                    { "hr", "دائرة الموارد البشرية" },
                    { "it_center", "مركز الحاسوب" },
                    { "postgraduate", "عمادة الدراسات العليا" },
                    { "presidency", "ديوان الرئاسة" },
                    { "procurement", "دائرة اللوازم والمشتريات" },
                    { "quality_unit", "وحدة التطوير وضمان الجودة" },
                    { "scientific_research", "عمادة البحث العلمي" },
                    { "student_affairs", "عمادة شؤون الطلبة" }
                });

            migrationBuilder.InsertData(
                table: "AuditItems",
                columns: new[] { "Id", "Category", "DepartmentId", "EvidencesJson", "IsoReference", "QuestionsJson", "Status", "Title", "WorkflowJson" },
                values: new object[,]
                {
                    { 100, "خاص بالمركز الصحي", "health_center", "نسخة الهيكل التنظيمي المعتمد والموقع للمركز الصحي||كشف بأسماء الكادر الطبي والتمريضي والإداري", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد يحدد العيادات والتمريض والصيدلية والاستقبال وخطوط السلطة والمسؤولية." },
                    { 101, "خاص بالمركز الصحي", "health_center", "بطاقات الوصف الوظيفي المعتمدة||عينة 3 موظفين (طبيب، ممرض، صيدلاني)||نسخ مزاولات المهنة السارية", "5.3 / 7.2", "", "بانتظار الرفع", "الوصف الوظيفي", "التحقق من أن لكل مسمى وظيفي بطاقة وصف وظيفي معتمدة وموقعة مع سريان تراخيص مزاولة المهنة الصحية." },
                    { 102, "خاص بالمركز الصحي", "health_center", "وثيقة السياسة الصحية وتعليمات المركز الصحي||شواهد إعلان السياسة الصحية داخل المركز وعلى الموقع", "5.2", "", "بانتظار الرفع", "السياسة الصحية", "التحقق من وجود سياسة صحية وتعليمات واضحة لتقديم الرعاية الصحية والتأمين الصحي ومعلنة ومفهومة." },
                    { 200, "خاص بالقبول والتسجيل", "admission", "نسخة الهيكل التنظيمي المعتمد والموقع||دليل تعميم الهيكل على موظفي الدائرة||كشف بأسماء الموظفين", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد للدائرة يوضح شعب القبول والتسجيل والوثائق ومطابقته للكادر الفعلي." },
                    { 201, "خاص بالقبول والتسجيل", "admission", "بطاقات الوصف الوظيفي لكافة الوظائف||عينة 3 موظفين من نماذج استلام الوصف الوظيفي الموقعة", "5.3 / 7.2", "", "بانتظار الرفع", "الوصف الوظيفي", "التحقق من وجود بطاقات وصف وظيفي معتمدة لمسجلي التخصصات وموظفي القبول والوثائق وتوقيعهم بالعلم." },
                    { 300, "خاص بالمورد البشرية", "hr", "نسخة الهيكل التنظيمي المعتمد والموقع||دليل تعميمه للموظفين||كشف بأسماء موظفي الدائرة", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لدائرة الموارد البشرية ومطابقته للكادر الفعلي." },
                    { 400, "خاص ديوان الرئاسة", "presidency", "سجل مصفوفة حصر الأطراف المعنية||آلية متابعة تلبية متطلبات الأطراف المعنية وتحديثها", "4.2", "", "بانتظار الرفع", "تحديد الأطراف المعنية", "التحقق من قيام ديوان الرئاسة بحصر كافة الجهات التي يتعامل معها وتحديد متطلبات كل جهة." },
                    { 500, "خاص شؤون الطلبة", "student_affairs", "نسخة الهيكل التنظيمي المعتمد والموقع للعمادة||دليل تعميمه للموظفين||كشف بأسماء الموظفين", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لعمادة شؤون الطلبة يوضح الأقسام وشعب وخطوط السلطة." },
                    { 600, "خاص مركز الحاسوب", "it_center", "نسخة الهيكل التنظيمي المعتمد والموقع لمركز الحاسوب||دليل تعميمه للموظفين", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لمركز الحاسوب يوضح الأقسام والشعب التقنية والبرمجية." },
                    { 700, "خاص وحدة الجودة", "quality_unit", "نسخة الهيكل التنظيمي المعتمد||قرارات تشكيل مجلس الجودة وضباط الارتباط||كشف بالأسماء", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لوحدة التطوير وضمان الجودة ولجان وضباط الارتباط." },
                    { 800, "خاص الدراسات العليا", "postgraduate", "نسخة الهيكل التنظيمي المعتمد||قرار تشكيل مجلس الدراسات العليا||كشف بأسماء كادر العمادة", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لعمادة الدراسات العليا ومجلس الدراسات العليا والأقسام التابعة." },
                    { 900, "خاص الكليات", "faculties", "نسخة الهيكل التنظيمي المعتمد||قرارات تشكيل مجلس الكلية ومجالس الأقسام||كشف بأعضاء الهيئة التدريسية", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد للكلية يوضح العمادة والمجالس والأقسام الأكاديمية واللجان." },
                    { 1000, "خاص البحث العلمي", "scientific_research", "نسخة الهيكل التنظيمي المعتمد||قرار تشكيل مجلس البحث العلمي وهيئات تحرير المجلات||كشف بالأسماء", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لعمادة البحث العلمي ومجلس البحث العلمي والمجلات العلمية." },
                    { 1100, "خاص اللوازم والمشتريات", "procurement", "نسخة الهيكل التنظيمي المعتمد والموقع للدائرة||دليل تعميمه للموظفين||كشف بأسماء الموظفين", "5.3", "", "بانتظار الرفع", "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لدائرة اللوازم والمشتريات يوضح شعب الشراء والمستودعات." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 201);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 300);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 400);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 500);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 600);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 700);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 800);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 900);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1000);

            migrationBuilder.DeleteData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1100);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "admission");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "faculties");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "health_center");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "hr");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "it_center");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "postgraduate");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "presidency");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "procurement");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "quality_unit");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "scientific_research");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "student_affairs");

            migrationBuilder.DropColumn(
                name: "IsoReference",
                table: "AuditItems");

            migrationBuilder.DropColumn(
                name: "WorkflowJson",
                table: "AuditItems");

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "EvidencesJson", "QuestionsJson" },
                values: new object[] { "بنود مشتركة", "نسخة الهيكل المعتمد||قرار الاعتماد||تعميم داخلي", "هل يوجد هيكل تنظيمي معتمد ومحدث؟||هل يوضح خطوط السلطة والمسؤوليات؟" });

            migrationBuilder.InsertData(
                table: "AuditItems",
                columns: new[] { "Id", "Category", "DepartmentId", "EvidencesJson", "QuestionsJson", "Status", "Title" },
                values: new object[] { 10, "خاص بالمكتبة", "library", "نظام المكتبة||تقارير الاستخدام", "هل يوجد نظام مكتبة فعال؟||هل يتم تحديثه وصيانته؟", "بانتظار الرفع", "نظام إدارة المكتبة" });
        }
    }
}

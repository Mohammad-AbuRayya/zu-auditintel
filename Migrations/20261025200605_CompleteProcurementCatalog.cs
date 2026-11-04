using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class CompleteProcurementCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1100,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "5.3", "الإعداد ← الاعتماد الرسمي ← الإعلان والتعميم ← المطابقة الفعالية : التحقق من وجود هيكل تنظيمي معتمد لدائرة اللوازم والمشتريات يوضح الشعب والأقسام (العطاءات، المستودعات، الاستلام) وخطوط الإشراف ومطابقته للكادر الحالي." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1101,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "5.3 / 7.2", "توصيف المهام ← الاعتماد ← التوقيع بالاستلام والعلم : التحقق من وجود بطاقات وصف وظيفي معتمدة لجميع المسميات الوظيفية في الدائرة (موظف العطاءات، أمين المستودع، موظف الشراء) وتوقيع الموظفين عليها." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1102,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "5.2 / 8.4", "الصياغة والاعتماد ← النشر والتعميم ← التطبيق في دورات الشراء : التحقق من وجود سياسة وتعليمات واضحة ومعتمدة للمشتريات واللوازم تنظم طلب الشراء، العطاء، الترسية، الاستلام والجرد." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1103,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "5.1 / 8.4", "تعهدات عدم تعارض المصالح ← تشكيل لجان الشراء والتقييم ← توثيق محاضر الجلسات وأصول فض العروض : التحقق من توفر ضوابط النزاهة والشفافية في إجراءات الشراء وانتفاء تعارض المصالح لدى أعضاء اللجان." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1104,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "8.4 / 7.5.3", "التسجيل والتأهيل ← التحقق من السجلات التجارية والضريبية ← التصنيف والتحديث الدوري : التحقق من ضبط ملفات الموردين وتحديث تأهيلهم قبل التعامل معهم." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1105,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "7.5.2 / 7.5.3", "الترقيم والإصدار ← التخزين والأرشفة ← تحديد مدة الحفظ وسهولة الاسترجاع : التحقق من ضبط سجلات الدائرة الورقية والإلكترونية وحفظها وفق قائمة الوثائق المعتمدة ومدد الحفظ." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1106,
                columns: new[] { "Category", "IsoReference", "Workflow" },
                values: new object[] { "Common", "7.2", "حصر الاحتياج التدريبي ← خطة التدريب ← التنفيذ ← قياس أثر التدريب : التحقق من كفاءة كادر الدائرة وتدريبهم على أنظمة المشتريات واللوازم وأصول العطاءات وتقييم أثر التدريب." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1107,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "6.2", "صياغة أهداف ذكية ← خطة العمل ← القياس الدوري : التحقق من وجود أهداف جودة قابلة للقياس للدائرة ومتابعة تحقيقها دورياً." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1108,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "4.1 / 6.2", "المواءمة الاستراتيجية ← الخطة التنفيذية السنوية ← تقارير الإنجاز : التحقق من ارتباط خطة الدائرة بالخطة الاستراتيجية للجامعة ومتابعة تنفيذ أنشطتها دورياً." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1109,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "9.1.1", "بطاقة المؤشر ← القياس الفعلي ← مقارنة المستهدف بالمتحقق ← خطة التحسين : التحقق من قياس مؤشرات الأداء الرئيسية للدائرة (مدة إنجاز معاملة الشراء، نسبة العطاءات المتأخرة، دقة المخزون) وتحليل نتائجها." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1110,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "4.1", "تحديد نقاط القوة والضعف والفرص والتهديدات ← الورقة التحليلية ← ربطها بالخطط : التحقق من إجراء الدائرة تحليل SWOT واستخدام مخرجاته في التخطيط." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1111,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "6.1", "رصد الفرص ← خطة استغلالها ← متابعة التنفيذ : التحقق من تحديد الدائرة للفرص ومعالجتها ضمن خطتها." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1112,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "6.1", "حصر المخاطر ← التقييم (الاحتمال والأثر) ← خطة المعالجة ← المتابعة : التحقق من تحديد مخاطر عمل الدائرة (تعطل التوريد، تأخر العطاءات، نفاد المخزون، تقادم الأصناف) ومعالجتها." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1113,
                columns: new[] { "Category", "IsoReference", "Workflow" },
                values: new object[] { "DepartmentSpecific", "8.1 / 8.4", "طلب الشراء من الجهة الطالبة ← التحقق من توفر رصيد الموازنة ← الموافقة والإحالة ← إصدار أمر الشراء : التحقق من ضبط دورة طلب الشراء وتوثيقها ومدة إنجازها." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1114,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "8.4", "الإعلان ونشر الدعوة ← بيع نسخة العطاء ← استلام العروض وفضّها ← الترسية ← العقد : التحقق من أصول طرح العطاءات ورسوها وفق الأنظمة النافذة." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1115,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "8.2.3 / 8.4", "تشكيل لجنة التقييم ← اعتماد معايير التقييم قبل الفتح ← تقييم العروض فنياً ومالياً ← إصدار التوصية : التحقق من عدالة تقييم العروض وموثوقية معاييره." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1116,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "8.4", "التأهيل والتصنيف ← متابعة الأداء والتقييم السنوي ← تعديل قائمة المعتمدين : التحقق من تقييم أداء الموردين ومراقبة التزامهم بشروط العقد." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1117,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "8.4 / 8.6", "تشكيل لجنة الاستلام ← الفحص والمطابقة للمواصفات ← التحقق من الكميات ← الإدخال للمستودع : التحقق من أصول استلام المواد وفحصها ومطابقتها لأمر الشراء." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1118,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "9.1.2 / 10.2", "استلام الشكوى ← التسجيل في السجل ← الدراسة والمعالجة ← الرد والإغلاق : التحقق من وجود آلية لتلقي شكاوى الموردين والجهات الطالبة ومعالجتها والرد عليها." });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1119,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "9.1.2", "إعداد الاستبانة ← توزيعها على الجهات الطالبة والموردين ← التحليل الإحصائي ← خطة التحسين : التحقق من قياس رضا المعنيين عن خدمات الدائرة وتحليل نتائجها واتخاذ إجراءات تحسينية." });

            migrationBuilder.InsertData(
                table: "EvidenceRequirements",
                columns: new[] { "Id", "AuditItemId", "SortOrder", "Text" },
                values: new object[,]
                {
                    { 1100501, 1100, 1, "نسخة الهيكل التنظيمي المعتمد والموقع لدائرة اللوازم والمشتريات" },
                    { 1100502, 1100, 2, "دليل تعميمه وإعلانه للموظفين" },
                    { 1100503, 1100, 3, "كشف بأسماء موظفي الدائرة ومسمياتهم وشعبهم الحالية" },
                    { 1101501, 1101, 1, "بطاقات الوصف الوظيفي المعتمدة لكافة وظائف الدائرة" },
                    { 1101502, 1101, 2, "عينة (3 موظفين) من نماذج استلام الوصف الوظيفي الموقعة والمؤرخة" },
                    { 1102501, 1102, 1, "وثيقة سياسة وتعليمات المشتريات واللوازم المعتمدة من الجهة المختصة" },
                    { 1102502, 1102, 2, "سجل توزيع السياسة وتعميمها على الموظفين والجهات الطالبة" },
                    { 1102503, 1102, 3, "شواهد تطبيق السياسة على عينات من أوامر الشراء المنفذة" },
                    { 1103501, 1103, 1, "نماذج تعهدات عدم تعارض المصالح والإفصاح الموقعة من أعضاء لجان الشراء والتقييم" },
                    { 1103502, 1103, 2, "محاضر جلسات اللجان موقعة وموثقة بأسماء الحاضرين" },
                    { 1103503, 1103, 3, "شواهد الإعلان عن العطاءات ونتائج الترسية على الجهات المعنية" },
                    { 1104501, 1104, 1, "قائمة الموردين المعتمدين والمصنّفين لدى الدائرة" },
                    { 1104502, 1104, 2, "ملف كامل لعينة (3) موردين يشمل السجل التجاري والرقم الضريبي واشتراك الضمان" },
                    { 1104503, 1104, 3, "تقارير تحديث القائمة أو شطب الموردين غير المؤهلين" },
                    { 1105501, 1105, 1, "القائمة الرئيسية للوثائق والسجلات المعتمدة في الدائرة (Master List) موضحاً فيها مدة الحفظ" },
                    { 1105502, 1105, 2, "شواهد أرشفة عينات من أوامر الشراء ومحاضر الاستلام وسجلات المستودع" },
                    { 1106501, 1106, 1, "نموذج تحديد الاحتياجات التدريبية لموظفي الدائرة" },
                    { 1106502, 1106, 2, "كشف الدورات التدريبية المنفذة وشهادات الحضور" },
                    { 1106503, 1106, 3, "نماذج تقييم أثر التدريب المعتمدة من مدير الدائرة" },
                    { 1107501, 1107, 1, "وثيقة أهداف الجودة المعتمدة لدائرة اللوازم والمشتريات للعام الحالي" },
                    { 1107502, 1107, 2, "خطة العمل لتحقيق أهداف الجودة" },
                    { 1107503, 1107, 3, "تقرير قياس نسبة تحقق أهداف الجودة" },
                    { 1108501, 1108, 1, "الخطة التنفيذية السنوية لدائرة اللوازم والمشتريات" },
                    { 1108502, 1108, 2, "تقارير متابعة الإنجاز الدورية ونسب التحقق" },
                    { 1109501, 1109, 1, "جدول/بطاقات مؤشرات الأداء المعتمدة للدائرة" },
                    { 1109502, 1109, 2, "تقرير القياس الفعلي للمؤشرات بالبيانات والإحصائيات" },
                    { 1109503, 1109, 3, "خطة التحسين للمؤشرات التي لم تصل للمستهدف" },
                    { 1110501, 1110, 1, "وثيقة تحليل SWOT المعتمدة للدائرة" },
                    { 1110502, 1110, 2, "محضر اجتماع مناقشة نتائج التحليل" },
                    { 1110503, 1110, 3, "مصفوفة ربط مخرجات التحليل بالخطة التنفيذية" },
                    { 1111501, 1111, 1, "سجل الفرص المعتمد للدائرة" },
                    { 1111502, 1111, 2, "خطة استغلال الفرص والإجراءات المقترحة" },
                    { 1111503, 1111, 3, "تقرير متابعة تنفيذ خطة الفرص" },
                    { 1112501, 1112, 1, "سجل المخاطر المعتمد للدائرة مع مصفوفة الاحتمال والأثر" },
                    { 1112502, 1112, 2, "خطة معالجة المخاطر والإجراءات الوقائية" },
                    { 1112503, 1112, 3, "تقرير المتابعة الدوري لفعالية المعالجة" },
                    { 1113501, 1113, 1, "طلبات شراء مكتملة الإجراءات (عينة 5) موقعة من الجهة الطالبة والمرجع المختص" },
                    { 1113502, 1113, 2, "شواهد التحقق من رصيد الموازنة قبل الإحالة" },
                    { 1113503, 1113, 3, "أوامر الشراء الصادرة ومطابقتها لبيانات الطلب الأصلي" },
                    { 1114501, 1114, 1, "نسخة من إعلان العطاء وشواهد نشره في الصحف ومنصة المشتريات" },
                    { 1114502, 1114, 2, "محضر استلام العروض وفضّ العروض موقعاً من اللجنة" },
                    { 1114503, 1114, 3, "محضر التوصية بالترسية وموافقة الجهة المختصة والعقد المبرم" },
                    { 1115501, 1115, 1, "قرار تشكيل لجنة التقييم وصلاحياتها" },
                    { 1115502, 1115, 2, "معايير التقييم الفنية والمالية المعتمدة قبل فتح العروض" },
                    { 1115503, 1115, 3, "كشوفات درجات التقييم ومقارنة العروض بالموازنة التقديرية" },
                    { 1116501, 1116, 1, "نماذج تقييم أداء الموردين للفترة الحالية" },
                    { 1116502, 1116, 2, "سجل المخالفات وحالات التأخير والإنذارات الموجهة للموردين" },
                    { 1116503, 1116, 3, "قائمة الموردين المعتمدين بعد التحديث والتصفية" },
                    { 1117501, 1117, 1, "محاضر لجان الاستلام والفحص موقعة من أعضائها" },
                    { 1117502, 1117, 2, "مطابقة المواصفات والكميات المستلمة مع أمر الشراء والعقد" },
                    { 1117503, 1117, 3, "سجلات الإدخال في المستودع وبطاقات الأصناف" },
                    { 1118501, 1118, 1, "سجل الشكاوى الخاص بالدائرة موضحاً فيه تاريخ الاستلام والرد" },
                    { 1118502, 1118, 2, "ملف معالجة عينة (3) شكاوى مع ما يثبت إغلاقها" },
                    { 1118503, 1118, 3, "تحليل إحصائي للشكاوى وأسبابها المتكررة" },
                    { 1119501, 1119, 1, "نموذج استبانة رضا المعنيين المعتمد" },
                    { 1119502, 1119, 2, "نتائج الاستبانة وتحليلها الإحصائي بالنسب المئوية" },
                    { 1119503, 1119, 3, "خطة التحسين المبنية على نتائج الرضا" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1100501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1100502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1100503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1101501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1101502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1102501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1102502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1102503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1103501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1103502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1103503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1104501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1104502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1104503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1105501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1105502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1106501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1106502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1106503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1107501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1107502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1107503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1108501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1108502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1109501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1109502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1109503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1110501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1110502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1110503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1111501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1111502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1111503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1112501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1112502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1112503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1113501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1113502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1113503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1114501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1114502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1114503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1115501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1115502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1115503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1116501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1116502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1116503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1117501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1117502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1117503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1118501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1118502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1118503);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1119501);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1119502);

            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1119503);

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1100,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1101,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1102,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1103,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1104,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1105,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1106,
                columns: new[] { "Category", "IsoReference", "Workflow" },
                values: new object[] { "DepartmentSpecific", "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1107,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1108,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1109,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1110,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1111,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1112,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1113,
                columns: new[] { "Category", "IsoReference", "Workflow" },
                values: new object[] { "Common", "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1114,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1115,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1116,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1117,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1118,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "AuditItems",
                keyColumn: "Id",
                keyValue: 1119,
                columns: new[] { "IsoReference", "Workflow" },
                values: new object[] { "", "" });
        }
    }
}

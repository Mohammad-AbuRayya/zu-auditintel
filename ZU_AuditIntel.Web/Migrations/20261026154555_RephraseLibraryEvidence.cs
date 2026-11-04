using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class RephraseLibraryEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1501,
                column: "Text",
                value: "نسخة الهيكل التنظيمي المعتمد والموقع للمكتبة للتأكد من وجود هيكل معتمد ومحدث");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1502,
                column: "Text",
                value: "دليل تعميم الهيكل وإعلانه للتأكد من إتاحته لجميع الموظفين");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1503,
                column: "Text",
                value: "كشف أسماء موظفي المكتبة ومسمياتهم وشعبهم الحالية للمطابقة مع الهيكل المعتمد");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 2501,
                column: "Text",
                value: "بطاقات الوصف الوظيفي المعتمدة وتحتوي على وصف وظيفي لكل مسمى في المكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 2502,
                column: "Text",
                value: "عينة (3 موظفين) من نماذج استلام الوصف الوظيفي الموقعة والمؤرخة لإثبات علم الموظف به");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 3501,
                column: "Text",
                value: "وثيقة سياسة وتعليمات المكتبة العامة المعتمدة للتأكد من اعتمادها من الجهة المختصة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 3502,
                column: "Text",
                value: "الدليل الإرشادي لمستخدمي المكتبة لإتاحة السياسة وتعليماتها للمستفيدين");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 5501,
                column: "Text",
                value: "تقارير فنية وإحصائية مستخرجة من نظام إدارة المكتبة الآلي لإثبات كفاءته ودقة بياناته");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 5503,
                column: "Text",
                value: "شواهد ضبط صلاحيات النظام وإجراء النسخ الاحتياطي لقاعدة بيانات المكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 6501,
                column: "Text",
                value: "نموذج تحديد الاحتياجات التدريبية لموظفي المكتبة لإثبات حصر الاحتياج");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 6502,
                column: "Text",
                value: "كشف الدورات التدريبية المنفذة وشهادات الحضور للمطابقة مع خطة التدريب");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 7501,
                column: "Text",
                value: "قائمة تفقد السلامة العامة وبيئة قاعات المطالعة للتأكد من فحصها دورياً ومعالجة ملاحظاتها");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 7502,
                column: "Text",
                value: "شواهد التسهيلات المخصصة للطلبة ذوي الاحتياجات الخاصة (مصاعد ومنحدرات، طاولات وأجهزة مهيأة، مساعدة بحثية)");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 8501,
                column: "Text",
                value: "وثيقة أهداف الجودة المعتمدة للمكتبة للعام الحالي بصيغة قابلة للقياس");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 8503,
                column: "Text",
                value: "تقرير قياس نسبة تحقق أهداف الجودة للتأكد من متابعتها دورياً");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 9501,
                column: "Text",
                value: "الخطة التنفيذية السنوية لدائرة المكتبة العامة للربط بالخطة الاستراتيجية للجامعة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 10501,
                column: "Text",
                value: "بطاقات مؤشرات الأداء المعتمدة للمكتبة بمستهدفات واضحة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 10502,
                column: "Text",
                value: "تقرير القياس الفعلي للمؤشرات بالبيانات والإحصائيات للتأكد من قياسها دورياً وتحليلها");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 11502,
                column: "Text",
                value: "محضر اجتماع فريق المكتبة لمناقشة التحليل وإقراره");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 12501,
                column: "Text",
                value: "سجل الفرص التحسينية للمكتبة وتوثيقها");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 12502,
                column: "Text",
                value: "خطة استثمار الفرص المعتمدة وشواهد متابعة تنفيذ المبادرات التطويرية");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 13501,
                column: "Text",
                value: "سجل المخاطر (Risk Register) المعتمد للمكتبة موضحاً احتمالية كل خطر وأثره");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 13503,
                column: "Text",
                value: "تقرير المتابعة الدورية وتحديث حالة المخاطر ومعالجاتها");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 14502,
                column: "Text",
                value: "كشوفات متابعة الكتب المتأخرة والإنذارات والغرامات المطبقة على المستفيدين");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 14503,
                column: "Text",
                value: "عينة من نماذج منح براءة الذمة للطلبة الخريجين والموظفين");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 15501,
                column: "Text",
                value: "قائمة قواعد البيانات والمصادر الإلكترونية المشترك بها للعام الحالي ووثائق سريانها وتجديدها");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 15503,
                column: "Text",
                value: "تقارير إحصائيات الدخول والتحميل الفعلية لقياس استخدام المصادر");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 16501,
                column: "Text",
                value: "سجل الكتب والدوريات المحولة للتجليد والترميم والصيانة لمعالجة التالف");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 17502,
                column: "Text",
                value: "سجل وإحصائيات خدمات الإرشاد المرجعي ومساعدة الباحثين وطلبة الدراسات العليا");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 18501,
                column: "Text",
                value: "نموذج وآلية تقديم الشكاوى والمقترحات في المكتبة وإعلان قنواتها");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 18502,
                column: "Text",
                value: "سجل حصر الشكاوى الواردة وحالة معالجتها ضمن المدة المحددة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 18503,
                column: "Text",
                value: "عينة لشكوى مغلقة توضح التحقيق والإجراء التصحيحي لمنع التكرار");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 19501,
                column: "Text",
                value: "نموذج استبانة قياس رضا المستفيدين عن خدمات المكتبة العامة بآلية معتمدة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 19502,
                column: "Text",
                value: "تقرير التحليل الإحصائي لنتائج الاستبانة للفصل أو العام الحالي");

            migrationBuilder.InsertData(
                table: "EvidenceRequirements",
                columns: new[] { "Id", "AuditItemId", "SortOrder", "Text" },
                values: new object[] { 2503, 2, 3, "سجل تحديث بطاقات الوصف الوظيفي للتأكد من أنها تُحدَّث بشكل دوري" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 2503);

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 1501,
                column: "Text",
                value: "نسخة الهيكل التنظيمي المعتمد والموقع لدائرة المكتبة العامة");

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
                keyValue: 2501,
                column: "Text",
                value: "بطاقات الوصف الوظيفي المعتمدة لكافة وظائف المكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 2502,
                column: "Text",
                value: "عينة (3 موظفين) من نماذج استلام الوصف الوظيفي الموقعة والمؤرخة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 3501,
                column: "Text",
                value: "وثيقة سياسة وتعليمات المكتبة العامة المعتمدة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 3502,
                column: "Text",
                value: "الدليل الإرشادي لمستخدمي المكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 5501,
                column: "Text",
                value: "تقارير فنية وإحصائية مستخرجة من نظام إدارة المكتبة الآلي");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 5503,
                column: "Text",
                value: "آلية ضبط صلاحيات النظام والنسخ الاحتياطي لقاعدة بيانات المكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 6501,
                column: "Text",
                value: "نموذج تحديد الاحتياجات التدريبية لموظفي المكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 6502,
                column: "Text",
                value: "كشف الدورات التدريبية المنفذة وشهادات الحضور");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 7501,
                column: "Text",
                value: "قائمة تفقد السلامة العامة وبيئة قاعات المطالعة في المكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 7502,
                column: "Text",
                value: "شواهد التسهيلات المخصصة للطلبة ذوي الاحتياجات الخاصة (مصاعد/منحدرات، طاولات وأجهزة مهيأة، مساعدة بحثية)");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 8501,
                column: "Text",
                value: "وثيقة أهداف الجودة المعتمدة للمكتبة للعام الحالي");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 8503,
                column: "Text",
                value: "تقرير قياس نسبة تحقق أهداف الجودة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 9501,
                column: "Text",
                value: "الخطة التنفيذية السنوية لدائرة المكتبة العامة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 10501,
                column: "Text",
                value: "جدول/بطاقات مؤشرات الأداء المعتمدة للمكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 10502,
                column: "Text",
                value: "تقرير القياس الفعلي للمؤشرات بالبيانات والإحصائيات");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 11502,
                column: "Text",
                value: "محضر اجتماع فريق المكتبة لمناقشة وإقرار التحليل");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 12501,
                column: "Text",
                value: "سجل/خطة استثمار الفرص المعتمدة للمكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 12502,
                column: "Text",
                value: "شواهد وتقارير متابعة تنفيذ المبادرات التطويرية");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 13501,
                column: "Text",
                value: "سجل المخاطر (Risk Register) المعتمد للمكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 13503,
                column: "Text",
                value: "تقرير المتابعة الدورية وتحديث حالة المخاطر");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 14502,
                column: "Text",
                value: "كشوفات متابعة الكتب المتأخرة والإنذارات/الغرامات المترتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 14503,
                column: "Text",
                value: "عينة من نماذج/إجراءات منح براءة الذمة للطلبة الخريجين والموظفين");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 15501,
                column: "Text",
                value: "قائمة قواعد البيانات والمصادر الإلكترونية المشترك بها للعام الحالي ووثائق سريانها");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 15503,
                column: "Text",
                value: "تقارير إحصائيات الدخول والتحميل الفعلية من قواعد البيانات الإلكترونية");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 16501,
                column: "Text",
                value: "سجل/كشف الكتب والدوريات المحولة للتجليد والترميم والصيانة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 17502,
                column: "Text",
                value: "سجل/إحصائيات خدمات الإرشاد المرجعي ومساعدة الباحثين وطلبة الدراسات العليا");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 18501,
                column: "Text",
                value: "آلية/نموذج تقديم الشكاوى والمقترحات في المكتبة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 18502,
                column: "Text",
                value: "سجل حصر الشكاوى الواردة وحالة معالجتها");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 18503,
                column: "Text",
                value: "عينة لشكوى مغلقة توضح التحقيق والإجراء التصحيحي المتخذ");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 19501,
                column: "Text",
                value: "نموذج استبانة قياس رضا المستفيدين عن خدمات المكتبة العامة");

            migrationBuilder.UpdateData(
                table: "EvidenceRequirements",
                keyColumn: "Id",
                keyValue: 19502,
                column: "Text",
                value: "تقرير التحليل الإحصائي لنتائج الاستبانة للفصل/العام الحالي");
        }
    }
}

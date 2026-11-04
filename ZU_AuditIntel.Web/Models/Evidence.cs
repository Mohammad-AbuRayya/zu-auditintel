namespace ZU_AuditIntel.Web.Models
{
    /// <summary>
    /// ورقة أدلة البنود: عمودٌ لكل وثيقة، ونصٌّ منسوب إلى صاحبه. مستقل تماماً: لا مفتاح أجنبياً
    /// إلى <see cref="AuditItem"/> ولا إلى <see cref="Department"/>، فحذف أي جدول آخر لا يمسّه،
    /// وحرّر لا يقيّد. عمودا التعريف نصٌّ منقول لا مرجع: يُقرأان ولا يُوصَلان.
    ///
    /// لا <c>HasData</c> هنا: الجدول ورقة عمل يعبّئها صاحبها بيده، والزرع المعلّق في نموذج EF
    /// يجعل كل تعديل لاحقاً على الكتالوج يولّد ترحيلاً يمحو ما كتبه هو.
    /// </summary>
    public class Evidence
    {
        public int Id { get; set; }

        /// <summary>اسم الجهة كما كان عند النقل، نصاً لا رقماً.</summary>
        public string DepartmentName { get; set; } = "";

        /// <summary>عنوان البند كما كان عند النقل، نصاً لا رقماً.</summary>
        public string ItemTitle { get; set; } = "";

        /// <summary>الأدلة المطلوبة بكتاب تعليمات التدقيق الداخلي.</summary>
        public string InstructionsEvidence { get; set; } = "";

        /// <summary>الأدلة المطلوبة بملف التدقيق الداخلي (قائمة الفحص المعتمدة ZU/QP02F003).</summary>
        public string InternalAuditEvidence { get; set; } = "";

        /// <summary>مسار العمل كما يرد في كتاب تعليمات التدقيق الداخلي.</summary>
        public string InstructionsWorkflow { get; set; } = "";

        /// <summary>الأسئلة الواردة في كتاب تعليمات التدقيق الداخلي.</summary>
        public string InstructionsQuestions { get; set; } = "";
    }
}

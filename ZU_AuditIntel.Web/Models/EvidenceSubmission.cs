namespace ZU_AuditIntel.Web.Models
{
    /// <summary>
    /// قرار وكيل الفحص الآلي بعد بوابة العيوب العشرة بالمائة: نسبة العيب 10% فأقل تعني
    /// «مقبول أولياً – بانتظار التدقيق البشري»، وأعلى تعني «مرفوض – يتطلب إجراءً تصحيحياً».
    /// الملف المرفوض لا يصل إلى شاشة التدقيق النهائي أصلاً.
    /// </summary>
    public enum AgentOutcome
    {
        NotChecked,
        Approved,
        Rejected
    }

    /// <summary>قرار التدقيق النهائي الذي يصدره رئيس وحدة التطوير وضمان الجودة.</summary>
    public enum FinalOutcome
    {
        NotReviewed,
        Approved,
        Rejected
    }

    /// <summary>
    /// ملف دليل رفعه ممثل جهة على بند من بنود جهته. يبقى في المخزن مع نتيجة الفحص الآلي
    /// وقرار التدقيق النهائي، وهو مصدر إحصاءات البنود المكتملة والباقية لكل جهة.
    /// </summary>
    public class EvidenceSubmission
    {
        public int Id { get; set; }

        public string DepartmentId { get; set; } = "";
        public Department? Department { get; set; }
        public int AuditItemId { get; set; }
        public AuditItem? AuditItem { get; set; }

        public string FileName { get; set; } = "";
        public string Extension { get; set; } = "";
        public long FileSize { get; set; }

        /// <summary>مسار نسبي داخل مخزن الأدلة، فلا ينكسر النقل بين جهاز وآخر.</summary>
        public string StoredPath { get; set; } = "";

        public string UploaderEmployeeId { get; set; } = "";
        public string UploaderName { get; set; } = "";
        public DateTime UploadedAt { get; set; }

        public int? AgentScore { get; set; }
        public int? AgentDefectRate { get; set; }
        public AgentOutcome AgentOutcome { get; set; }

        /// <summary>نص ملاحظات الوكيل كما عُرض على الجهة، ويُسلَّم للمدقق النهائي مع الملف.</summary>
        public string? AgentFindings { get; set; }
        public DateTime? AgentCheckedAt { get; set; }

        /// <summary>
        /// الأدلة التي أشار إليها ممثل الجهة عند صدور القرار، مفصولة بأسطر. قرار الوكيل على البند
        /// كله لا على ملف منه، فيُكتب على كل صفوف البند ويُلغى جميعها معاً.
        /// </summary>
        public string? ProvidedEvidence { get; set; }

        public FinalOutcome FinalOutcome { get; set; }
        public string? FinalNote { get; set; }
        public string? ReviewerEmployeeId { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}

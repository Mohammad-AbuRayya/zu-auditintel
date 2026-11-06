using Google.Cloud.Firestore;
using ZU_AuditIntel.Web.Data;

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
    /// ملف دليل رفعه ممثل جهة على بند من بنود جهته، ووثيقته في submissions تحت رقم عشوائي من
    /// Firestore. يبقى مع نتيجة الفحص الآلي وقرار التدقيق النهائي، وهو مصدر إحصاءات البنود
    /// المكتملة والباقية لكل جهة. الأرقام الوظيفية محفوظة فيه نصاً، لا مراجع تُحلّ وقت القراءة.
    /// </summary>
    [FirestoreData]
    public class EvidenceSubmission
    {
        /// <summary>معرّف الوثيقة نفسه: لا يُكتب إلى حقل، بل يقرأ منها.</summary>
        [FirestoreDocumentId]
        public string Id { get; set; } = "";

        [FirestoreProperty("departmentId")]
        public string DepartmentId { get; set; } = "";

        [FirestoreProperty("auditItemId")]
        public int AuditItemId { get; set; }

        [FirestoreProperty("fileName")]
        public string FileName { get; set; } = "";

        [FirestoreProperty("extension")]
        public string Extension { get; set; } = "";

        [FirestoreProperty("fileSize")]
        public long FileSize { get; set; }

        /// <summary>مسار نسبي داخل مخزن الأدلة، فلا ينكسر النقل بين جهاز وآخر.</summary>
        [FirestoreProperty("storedPath")]
        public string StoredPath { get; set; } = "";

        [FirestoreProperty("uploaderEmployeeId")]
        public string UploaderEmployeeId { get; set; } = "";

        [FirestoreProperty("uploaderName")]
        public string UploaderName { get; set; } = "";

        [FirestoreProperty("uploadedAt", ConverterType = typeof(LocalTimeConverter))]
        public DateTime UploadedAt { get; set; }

        [FirestoreProperty("agentScore")]
        public int? AgentScore { get; set; }

        [FirestoreProperty("agentDefectRate")]
        public int? AgentDefectRate { get; set; }

        [FirestoreProperty("agentOutcome", ConverterType = typeof(FirestoreEnumNameConverter<AgentOutcome>))]
        public AgentOutcome AgentOutcome { get; set; }

        /// <summary>نص ملاحظات الوكيل كما عُرض على الجهة، ويُسلَّم للمدقق النهائي مع الملف.</summary>
        [FirestoreProperty("agentFindings")]
        public string? AgentFindings { get; set; }

        [FirestoreProperty("agentCheckedAt", ConverterType = typeof(LocalTimeConverter))]
        public DateTime? AgentCheckedAt { get; set; }

        /// <summary>
        /// الأدلة التي أشار إليها ممثل الجهة عند صدور القرار، مفصولة بأسطر. قرار الوكيل على البند
        /// كله لا على ملف منه، فيُكتب على كل صفوف البند ويُلغى جميعها معاً.
        /// </summary>
        [FirestoreProperty("providedEvidence")]
        public string? ProvidedEvidence { get; set; }

        [FirestoreProperty("finalOutcome", ConverterType = typeof(FirestoreEnumNameConverter<FinalOutcome>))]
        public FinalOutcome FinalOutcome { get; set; }

        [FirestoreProperty("finalNote")]
        public string? FinalNote { get; set; }

        [FirestoreProperty("reviewerEmployeeId")]
        public string? ReviewerEmployeeId { get; set; }

        [FirestoreProperty("reviewerName")]
        public string? ReviewerName { get; set; }

        [FirestoreProperty("reviewedAt", ConverterType = typeof(LocalTimeConverter))]
        public DateTime? ReviewedAt { get; set; }
    }
}

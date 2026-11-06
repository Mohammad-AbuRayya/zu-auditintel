using Google.Cloud.Firestore;

namespace ZU_AuditIntel.Web.Models
{
    /// <summary>تحديد ما إذا كان البند مشتركاً بين الجهات أم خاصاً بالجهة المدققة.</summary>
    public enum AuditItemCategory
    {
        Common,
        DepartmentSpecific
    }

    /// <summary>
    /// ورقة أدلة البند مضمّنة في وثيقته: كانت جدولاً يقابل باسم الجهة وعنوان البند، فصارت قراءةً
    /// واحدة بلا مقابل. والأعمدة نصٌّ منقول لا مرجع، لا يُوصَل بجدول ولا يُقرأ منه حساب.
    /// </summary>
    [FirestoreData]
    public sealed class AuditItemSheet
    {
        /// <summary>الأدلة المطلوبة بكتاب تعليمات التدقيق الداخلي.</summary>
        [FirestoreProperty("instructionsEvidence")]
        public string InstructionsEvidence { get; set; } = "";

        /// <summary>الأدلة المطلوبة بملف التدقيق الداخلي (قائمة الفحص المعتمدة ZU/QP02F003).</summary>
        [FirestoreProperty("internalAuditEvidence")]
        public string InternalAuditEvidence { get; set; } = "";

        /// <summary>مسار العمل كما يرد في كتاب تعليمات التدقيق الداخلي.</summary>
        [FirestoreProperty("instructionsWorkflow")]
        public string InstructionsWorkflow { get; set; } = "";

        /// <summary>الأسئلة الواردة في كتاب تعليمات التدقيق الداخلي.</summary>
        [FirestoreProperty("instructionsQuestions")]
        public string InstructionsQuestions { get; set; } = "";
    }

    /// <summary>
    /// تعريف بند تدقيق (بيانات كتالوج ثابتة) ببنوده المطلوبة وأسئلته مضمّنة فيه، بترتيب العرض.
    /// نتائج التدقيق التشغيلية ليست هنا: ملفات الأدلة وقرار الوكيل وقرار التدقيق النهائي تعيش في
    /// <see cref="EvidenceSubmission"/>، فوثيقة البند لا يكتب عليها إلا مصدّر الكتالوج.
    /// </summary>
    [FirestoreData]
    public class AuditItem
    {
        [FirestoreProperty("id")]
        public int Id { get; set; }

        [FirestoreProperty("departmentId")]
        public string DepartmentId { get; set; } = "";

        [FirestoreProperty("category", ConverterType = typeof(FirestoreEnumNameConverter<AuditItemCategory>))]
        public AuditItemCategory Category { get; set; }

        [FirestoreProperty("title")]
        public string Title { get; set; } = "";

        [FirestoreProperty("isoReference")]
        public string IsoReference { get; set; } = "";

        [FirestoreProperty("workflow")]
        public string Workflow { get; set; } = "";

        /// <summary>ترتيب البند في لوحة جهته؛ أرقام لوحات الجهات مستقلة عن ترقيم الزرع العام.</summary>
        [FirestoreProperty("sortOrder")]
        public int SortOrder { get; set; }

        [FirestoreProperty("requiredEvidence")]
        public List<string> RequiredEvidence { get; set; } = [];

        [FirestoreProperty("questions")]
        public List<string> Questions { get; set; } = [];

        [FirestoreProperty("sheet")]
        public AuditItemSheet Sheet { get; set; } = new();
    }
}

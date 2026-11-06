using Google.Cloud.Firestore;
using ZU_AuditIntel.Web.Data;

namespace ZU_AuditIntel.Web.Models
{
    /// <summary>
    /// ما يستحق أن يصل إلى صاحب الحساب: رفع ملف، نتيجة الفحص الآلي، تجاوز إداري، ثم قبول المدقق
    /// أو رفضه. الحالات مسمّاة بأسمائها لأن صفحة الإشعارات تبني منها العبارة في لغة القارئ.
    /// </summary>
    public enum NotificationKind
    {
        /// <summary>رُفع ملف دليل على بند: يصل إلى الجهة كلها لأن الرفع حدث يخصها لا شخصاً واحداً فيها.</summary>
        Uploaded,

        /// <summary>أجاز وكيل الفحص الآلي ملفات البند، فصار بانتظار التدقيق البشري.</summary>
        AgentPassed,

        /// <summary>لم تُجز ملفات البند من الوكيل، فعادت إلى الجهة لإجراء تصحيحي.</summary>
        AgentFailed,

        /// <summary>تجاوز مدير النظام البوابة الآلية وأجاز البند بيده، فلا درجة تُنسب إلى وكيل.</summary>
        Overridden,

        /// <summary>قَبِل المدقق البشري ملفاً، فإن كان آخر ملف في بنده صار البند معتمداً نهائياً.</summary>
        Approved,

        /// <summary>رفض المدقق البشري ملفاً بسبب مكتوب، فعاد البند إلى الجهة.</summary>
        Rejected
    }

    /// <summary>
    /// سطر إشعار واحد: يصف حدثاً على بند، مع أجزائه محفوظة كما هي (الجهة، البند، الملف، صاحبه،
    /// الملاحظة) لا جملةً عربية جاهزة، فتُركَّب العبارة في لغة القارئ وقت العرض. لا علاقة له بغيره
    /// من الجداول: سجل يُقرأ ولا يُبنى عليه حساب، فيبقى بعد حذف الملف الذي تكلّم عنه.
    /// </summary>
    [FirestoreData]
    public class Notification
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = "";

        [FirestoreProperty("kind", ConverterType = typeof(FirestoreEnumNameConverter<NotificationKind>))]
        public NotificationKind Kind { get; set; }

        /// <summary>جهة البند، وهي مفتاح الرؤية: صاحب الجهة يقرأ إشعارات جهته وحدها.</summary>
        [FirestoreProperty("departmentId")]
        public string DepartmentId { get; set; } = "";

        [FirestoreProperty("departmentName")]
        public string DepartmentName { get; set; } = "";

        [FirestoreProperty("itemTitle")]
        public string ItemTitle { get; set; } = "";

        [FirestoreProperty("fileName")]
        public string? FileName { get; set; }

        /// <summary>من أحدث الحدث: رافع الملف، أو المدقق، أو من تجاوز البوابة. لا اسم لوكيل الفحص.</summary>
        [FirestoreProperty("actorName")]
        public string? ActorName { get; set; }

        /// <summary>درجة الفحص الآلي حين وُجدت، أو سبب الرفض البشري، أو ملاحظة التجاوز.</summary>
        [FirestoreProperty("agentScore")]
        public int? AgentScore { get; set; }

        [FirestoreProperty("note")]
        public string? Note { get; set; }

        [FirestoreProperty("createdAt", ConverterType = typeof(LocalTimeConverter))]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// من قرأ السطر من أصحاب الحسابات، أرقاماً لا سطراً مستقلاً لكل قراءة: لكل صاحب حساب
        /// قرّاؤه، فلا يمحو فتحُ ممثل الجهة للصفحة إشعاراتِ مدقّقها.
        /// </summary>
        [FirestoreProperty("readBy")]
        public List<string> ReadBy { get; set; } = [];

        /// <summary>
        /// دوائر الرؤية تُكتب وقت الحدث: «dept:…» لكل سطر، و«kind:Approved» لما أُغلق
        /// معتمداً. تستعلام الصفحة بـ array-contains على حقلٍ واحد، فلا تحتاج فهرساً مركّباً
        /// ولا سبيل إلى إنشاء فهرس كهذا بلا وصول إلى التحكّم.
        /// </summary>
        [FirestoreProperty("visibleTo")]
        public List<string> VisibleTo { get; set; } = [];
    }
}

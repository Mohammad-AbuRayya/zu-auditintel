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
    public class Notification
    {
        public int Id { get; set; }
        public NotificationKind Kind { get; set; }

        /// <summary>جهة البند، وهي مفتاح الرؤية: صاحب الجهة يقرأ إشعارات جهته وحدها.</summary>
        public string DepartmentId { get; set; } = "";
        public string DepartmentName { get; set; } = "";

        public string ItemTitle { get; set; } = "";
        public string? FileName { get; set; }

        /// <summary>من أحدث الحدث: رافع الملف، أو المدقق، أو من تجاوز البوابة. لا اسم لوكيل الفحص.</summary>
        public string? ActorName { get; set; }

        /// <summary>درجة الفحص الآلي حين وُجدت، أو سبب الرفض البشري، أو ملاحظة التجاوز.</summary>
        public int? AgentScore { get; set; }
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// قراءة إشعار لحساب واحد: لكل صاحب حساب سجلّه، فلا يمحو فتحُ ممثل الجهة للصفحة إشعاراتِ
    /// مدقّقها. المفتاح مركّب من الإشعار والحساب، فلا تتكرر قراءة واحدة.
    /// </summary>
    public class NotificationRead
    {
        public int NotificationId { get; set; }
        public string EmployeeId { get; set; } = "";
        public DateTime ReadAt { get; set; }
    }
}

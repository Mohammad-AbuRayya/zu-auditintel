namespace ZU_AuditIntel.Web.Models
{
    /// <summary>
    /// محادثة ثنائية بين حسابين. <see cref="PairKey" /> يمنع فتح محادثتين بين الشخصين أنفسهما،
    /// فالعنوان واحد والرسائل في مكان واحد مهما بدأها أحدهما أو الآخر.
    /// </summary>
    public class MessageThread
    {
        public int Id { get; set; }

        /// <summary>رقما الحسابين مرتبين ترتيباً نصياً ومفصولين بـ «|»: هوية المحادثة ولا يُعرض.</summary>
        public string PairKey { get; set; } = "";
        public string EmployeeIdA { get; set; } = "";
        public string EmployeeIdB { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        /// <summary>وقت آخر رسالة: به تُرتَّب القائمة، ولا يُحسب من السطور عند كل قراءة.</summary>
        public DateTime LastMessageAt { get; set; }

        public List<Message> Messages { get; set; } = new();
    }

    /// <summary>
    /// رسالة واحدة داخل محادثة. اسم المرسل ومسمّاه محفوظان معها كما كانا لحظة الإرسال، فسطرٌ
    /// قديم لا يتغيّر إذا تغيّر مسمّى صاحبه، ويبقى مقروءاً ولو عُطّل حسابه بعدها.
    /// </summary>
    public class Message
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public MessageThread Thread { get; set; } = null!;

        public string SenderEmployeeId { get; set; } = "";
        public string SenderName { get; set; } = "";

        /// <summary>نص الرسالة كما كتبه صاحبها، بلا تعديل ولا صياغة.</summary>
        public string Body { get; set; } = "";
        public DateTime SentAt { get; set; }

        /// <summary>وقت قراءة الطرف الآخر لها؛ null ما دامت لم تُقرأ.</summary>
        public DateTime? ReadAt { get; set; }
    }
}

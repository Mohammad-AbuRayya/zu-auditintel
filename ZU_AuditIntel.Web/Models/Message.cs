using Google.Cloud.Firestore;
using ZU_AuditIntel.Web.Data;

namespace ZU_AuditIntel.Web.Models
{
    /// <summary>
    /// محادثة ثنائية بين حسابين، ووثيقتها في threads مفتاحها <see cref="PairKey" />: رقمان مرتبان
    /// نصياً مفصولان بـ «|». العنوان واحد والرسائل في مكان واحد مهما بدأها أحدهما أو الآخر، فلا
    /// محادثتين بين الشخصين نفسيهما. وآخر ما قيل محفوظ هنا لأن قائمة المحادثات تقرأ الوثيقة وحدها
    /// ولا تنزل إلى سطور الرسائل واحداً واحداً.
    /// </summary>
    [FirestoreData]
    public class MessageThread
    {
        [FirestoreDocumentId]
        public string PairKey { get; set; } = "";

        /// <summary>الرقمان مرتبان ترتيباً نصياً، فـ A هو الأصح نصاً دائماً.</summary>
        [FirestoreProperty("employeeIdA")]
        public string EmployeeIdA { get; set; } = "";

        [FirestoreProperty("employeeIdB")]
        public string EmployeeIdB { get; set; } = "";

        /// <summary>الطرفان في مصفوفة واحدة: قائمة محادثات الحساب سؤال «تحوي» واحد بلا فهرس مركّب.</summary>
        [FirestoreProperty("participants")]
        public List<string> Participants { get; set; } = [];

        [FirestoreProperty("createdAt", ConverterType = typeof(LocalTimeConverter))]
        public DateTime CreatedAt { get; set; }

        /// <summary>وقت آخر رسالة: به تُرتَّب القائمة، ولا يُحسب من السطور عند كل قراءة.</summary>
        [FirestoreProperty("lastMessageAt", ConverterType = typeof(LocalTimeConverter))]
        public DateTime LastMessageAt { get; set; }

        [FirestoreProperty("lastBody")]
        public string LastBody { get; set; } = "";

        [FirestoreProperty("lastSenderEmployeeId")]
        public string LastSenderEmployeeId { get; set; } = "";

        /// <summary>
        /// ما لم يقرأه كل طرف بعدُ، عدّادان على الوثيقة لا سطر قراءة لكل رسالة: الشارة تُحسب من
        /// الوثيقة وحدها، والقراءة تُصفّر عدّاد القارئ فقط.
        /// </summary>
        [FirestoreProperty("unreadForA")]
        public int UnreadForA { get; set; }

        [FirestoreProperty("unreadForB")]
        public int UnreadForB { get; set; }

        /// <summary>عدد ما لم يقرأه هذا الحساب، بمن أيّ الطرفين كان.</summary>
        public int UnreadFor(string employeeId) =>
            employeeId == EmployeeIdA ? UnreadForA : employeeId == EmployeeIdB ? UnreadForB : 0;

        /// <summary>الطرف الآخر بالنسبة لسائل، أو null إن لم يكن من الطرفين.</summary>
        public string? OtherOf(string employeeId) =>
            employeeId == EmployeeIdA ? EmployeeIdB : employeeId == EmployeeIdB ? EmployeeIdA : null;

        public void BumpUnread(string senderEmployeeId)
        {
            if (senderEmployeeId == EmployeeIdA)
                UnreadForB++;
            else
                UnreadForA++;
        }

        public void ClearUnread(string employeeId)
        {
            if (employeeId == EmployeeIdA)
                UnreadForA = 0;
            else if (employeeId == EmployeeIdB)
                UnreadForB = 0;
        }
    }

    /// <summary>
    /// رسالة واحدة داخل محادثة، ووثيقتها في threads/{pairKey}/messages. اسم المرسل ومسمّاه
    /// محفوظان معها كما كانا لحظة الإرسال، فسطرٌ قديم لا يتغيّر إذا تغيّر مسمّى صاحبه، ويبقى
    /// مقروءاً ولو عُطّل حسابه بعدها.
    /// </summary>
    [FirestoreData]
    public class Message
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = "";

        [FirestoreProperty("senderEmployeeId")]
        public string SenderEmployeeId { get; set; } = "";

        [FirestoreProperty("senderName")]
        public string SenderName { get; set; } = "";

        /// <summary>نص الرسالة كما كتبه صاحبها، بلا تعديل ولا صياغة.</summary>
        [FirestoreProperty("body")]
        public string Body { get; set; } = "";

        [FirestoreProperty("sentAt", ConverterType = typeof(LocalTimeConverter))]
        public DateTime SentAt { get; set; }
    }
}

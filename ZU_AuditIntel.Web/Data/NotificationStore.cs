using System.Security.Claims;
using Google.Cloud.Firestore;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// عين القارئ على السجل: صاحب الجهة يقرأ أحداث جهته وحدها، ورئيس الوحدة ومدير النظام يقرآن
    /// ما جرى في الجامعة كلها، وصاحب النتيجة لا يقرأ إلا ما أُغلق «معتمداً».
    /// </summary>
    public sealed record NotificationViewer(string EmployeeId, string? DepartmentId, bool SeesAllDepartments, bool ResultsOnly)
    {
        public static NotificationViewer From(ClaimsPrincipal user) => new(
            user.FindFirstValue(AuditClaims.EmployeeId) ?? "",
            user.DepartmentId(),
            user.IsQualityHead() || user.IsSystemAdministrator(),
            user.IsFinalResultReader());
    }

    /// <summary>سطر إشعار كما تعرضه الصفحة: أجزاء الحدث، ومعها هل قرأه هذا الحساب قبل اليوم.</summary>
    public sealed record NotificationRow(
        string Id,
        NotificationKind Kind,
        string DepartmentId,
        string DepartmentName,
        string ItemTitle,
        string? FileName,
        string? ActorName,
        int? AgentScore,
        string? Note,
        DateTime CreatedAt,
        bool IsNew);

    public interface INotificationStore
    {
        /// <summary>أحدث ما يراه هذا الحساب، مع بادرة «جديد» على ما لم يُقرأ بعد.</summary>
        Task<IReadOnlyList<NotificationRow>> ListAsync(
            NotificationViewer viewer,
            CancellationToken cancellationToken = default);

        /// <summary>عدد ما يراه ولم يقرأه: به يظهر الرقم بجانب زر الإشعارات.</summary>
        Task<int> CountUnreadAsync(NotificationViewer viewer, CancellationToken cancellationToken = default);

        /// <summary>يفتح الصفحة فيقرأ صاحبها ما فيها: تُكتب قراءة لكل سطر ظاهر لم يُقرأ.</summary>
        Task MarkAllReadAsync(NotificationViewer viewer, CancellationToken cancellationToken = default);

        /// <summary>
        /// سطر حدث يُكتب وقت وقوعه. المخزن هو من يحسب دوائر الرؤية من الجهة والقرار، فلا يكرّر
        /// كاتبُ الحدث قاعدةً تخصّ طريقة القراءة ولا يكتب إشعاراً يراه من لا يجوز له.
        /// </summary>
        Task RecordAsync(Notification notification, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// سجل الإشعارات على Firestore. الرؤية حقلٌ مكتوب لا شرطٌ يُبنى: «dept:hr» و«kind:Approved»
    /// داخل مصفوفة واحدة، فتُغني استعلامة array-contains مفردة عن فهرس مركّب، والترتيب في
    /// الذاكرة على مئتي سطر لا أكثر.
    /// </summary>
    public sealed class FirestoreNotificationStore(FirestoreDb firestore) : INotificationStore
    {
        /// <summary>سقف ما تُعرضه الصفحة؛ السجل يطول مع رفع كل ملف، والصفحة للجديد لا لأرشيف كامل.</summary>
        public const int MaxRows = 200;

        /// <summary>أقصى ما يُقرأ من الواجهة في زيارة صفحة: ما وراءه لا يُعرض ولا يُعدّ مقروءاً.</summary>
        private const int ScanLimit = 2000;

        private CollectionReference Notifications { get; } = firestore.Collection(AuditCollections.Notifications);

        public async Task<IReadOnlyList<NotificationRow>> ListAsync(
            NotificationViewer viewer,
            CancellationToken cancellationToken = default)
        {
            var documents = await VisibleAsync(viewer, cancellationToken);
            var employeeId = viewer.EmployeeId;

            var newest = documents
                .Select(snapshot => (Notification: snapshot.ConvertTo<Notification>()!, DocId: snapshot.Id))
                .OrderByDescending(pair => pair.Notification.CreatedAt)
                .ThenByDescending(pair => pair.DocId, StringComparer.Ordinal)
                .Take(MaxRows);

            return newest
                .Select(pair =>
                {
                    var n = pair.Notification;
                    return new NotificationRow(
                        pair.DocId,
                        n.Kind,
                        n.DepartmentId,
                        n.DepartmentName,
                        n.ItemTitle,
                        n.FileName,
                        n.ActorName,
                        n.AgentScore,
                        n.Note,
                        n.CreatedAt,
                        !n.ReadBy.Contains(employeeId));
                })
                .ToList();
        }

        public async Task<int> CountUnreadAsync(
            NotificationViewer viewer, CancellationToken cancellationToken = default)
        {
            if (viewer.EmployeeId.Length == 0)
                return 0;

            var documents = await VisibleAsync(viewer, cancellationToken);

            return documents
                .Select(snapshot => snapshot.ConvertTo<Notification>()!)
                .Count(n => !n.ReadBy.Contains(viewer.EmployeeId));
        }

        public async Task MarkAllReadAsync(
            NotificationViewer viewer, CancellationToken cancellationToken = default)
        {
            if (viewer.EmployeeId.Length == 0)
                return;

            var unread = (await VisibleAsync(viewer, cancellationToken))
                .Where(snapshot => !snapshot.ConvertTo<Notification>()!.ReadBy.Contains(viewer.EmployeeId))
                .ToList();

            if (unread.Count == 0)
                return;

            var batch = firestore.StartBatch();
            foreach (var snapshot in unread)
            {
                batch.Update(snapshot.Reference, new Dictionary<string, object>
                {
                    ["readBy"] = FieldValue.ArrayUnion([viewer.EmployeeId])
                });
            }

            await batch.CommitAsync(cancellationToken);
        }

        public async Task RecordAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            notification.VisibleTo = Audience(notification.Kind, notification.DepartmentId);
            notification.ReadBy = [];

            var reference = notification.Id.Length == 0
                ? Notifications.Document()
                : Notifications.Document(notification.Id);

            await reference.SetAsync(notification, SetOptions.Overwrite, cancellationToken);
        }

        /// <summary>
        /// دوائر من يحق له رؤية هذا السطر. جهةُ الحدث دائرته دائماً، و«kind:Approved» دائرة صاحب
        /// النتيجة وحده، فهي الاستثناء الوحيد الذي يقرأ من خارج جهته: ما أُغلق نهائياً على مستوى
        /// الجامعة. لا دائرة عامة تُكتب، فرئيس الوحدة ومدير النظام يقرآن بلا استعلام أصلاً.
        /// </summary>
        private static List<string> Audience(NotificationKind kind, string departmentId)
        {
            var audience = new List<string> { $"dept:{departmentId}" };
            if (kind == NotificationKind.Approved)
                audience.Add("kind:Approved");

            return audience;
        }

        /// <summary>
        /// ما يحق لهذا الحساب رؤيته، استعلاماً واحداً بلا فهرس مركّب: من لا جهة له ولا صفة تفتح
        /// السجل كله لا يرى شيئاً بدل أن يرى إشعارات جهة ليست جهته، ومن يقرأ الجامعة كلها بلا
        /// شرط أصلاً فيكفيه ترتيبٌ على حقل واحد. المقروء سقفٌ واحد، والترتيب بعد ذلك في الذاكرة.
        /// </summary>
        private async Task<IReadOnlyList<DocumentSnapshot>> VisibleAsync(
            NotificationViewer viewer, CancellationToken cancellationToken)
        {
            Query query;

            if (viewer.SeesAllDepartments)
                query = Notifications.OrderByDescending("createdAt");
            else if (viewer.DepartmentId is not null)
                query = Notifications.WhereArrayContains("visibleTo", $"dept:{viewer.DepartmentId}");
            else if (viewer.ResultsOnly)
                query = Notifications.WhereArrayContains("visibleTo", "kind:Approved");
            else
                return [];

            var snapshot = await query.Limit(ScanLimit).GetSnapshotAsync(cancellationToken);
            return snapshot.Documents;
        }
    }
}

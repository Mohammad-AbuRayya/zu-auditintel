using System.Security.Claims;
using Google.Cloud.Firestore;
using Grpc.Core;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// عين المرسل: من هو، وأي جهة، وأي دور. بها تُبنى قائمة من يحق له مراسلتهم، وتحرس المخزن
    /// نفسه فلا تُترك الصلاحية لترشيح الواجهة وحده.
    /// </summary>
    public sealed record MessageViewer(string EmployeeId, string FullName, string? DepartmentId, AuditorRole? Role)
    {
        public static MessageViewer From(ClaimsPrincipal user)
        {
            var roleText = user.FindFirstValue(ClaimTypes.Role);
            var role = Enum.TryParse<AuditorRole>(roleText, out var parsed) ? parsed : (AuditorRole?)null;

            return new MessageViewer(
                user.FindFirstValue(AuditClaims.EmployeeId) ?? "",
                user.Identity?.Name ?? "",
                user.DepartmentId(),
                role);
        }

        /// <summary>رئيس الوحدة وصاحب النتيجة والمدير: يراسلون الكل ويقبلون مراسلة الكل.</summary>
        public bool ReachesEveryone =>
            Role is AuditorRole.QualityHead or AuditorRole.FinalResultReader or AuditorRole.SystemAdministrator;

        public bool IsRepresentative => Role == AuditorRole.EntityRepresentative;

        public bool IsEntityAuditor => Role == AuditorRole.EntityAuditor;
    }

    /// <summary>جهة اتصال كما تُعرض في قائمة الاختيار: الاسم والمسمّى والجهة والدور.</summary>
    public sealed record MessageContact(
        string EmployeeId,
        string FullName,
        string JobTitle,
        string? DepartmentId,
        string? DepartmentName,
        AuditorRole Role);

    /// <summary>سطر محادثة في قائمة المحادثات: الطرف الآخر، وآخر ما قيل، ومقدار ما لم يُقرأ.</summary>
    public sealed record MessageThreadRow(
        string OtherEmployeeId,
        string OtherName,
        string OtherJobTitle,
        string? OtherDepartmentName,
        string LastBody,
        bool LastFromMe,
        DateTime LastAt,
        int UnreadCount);

    public sealed record MessageRow(string Id, bool IsMine, string SenderName, string Body, DateTime SentAt);

    /// <summary>المحادثة مفتوحة: طرفها الآخر وسطورها بالترتيب الزمني.</summary>
    public sealed record MessageThreadView(MessageContact Other, IReadOnlyList<MessageRow> Messages);

    public interface IMessageStore
    {
        /// <summary>محادثات صاحب الحساب، الأحدث أولاً، ومقدار غير المقروء في كل واحدة.</summary>
        Task<IReadOnlyList<MessageThreadRow>> ListThreadsAsync(
            MessageViewer viewer,
            CancellationToken cancellationToken = default);

        /// <summary>عدد الرسائل التي وصلته ولم يقرأها: به تظهر الشارة بجانب زر الرسائل.</summary>
        Task<int> CountUnreadAsync(string employeeId, CancellationToken cancellationToken = default);

        /// <summary>من يحق لهذا الحساب مراسلتهم، ومن راسله سابقاً ولو خرج من قواعد الدور.</summary>
        Task<IReadOnlyList<MessageContact>> ListContactsAsync(
            MessageViewer viewer,
            CancellationToken cancellationToken = default);

        /// <summary>محادثة قائمة، أو null إن لم يكن بينهما شيء بعد. وفتحها قراءةٌ لما وصل فيها.</summary>
        Task<MessageThreadView?> GetThreadAsync(
            MessageViewer viewer,
            string otherEmployeeId,
            CancellationToken cancellationToken = default);

        /// <summary>إرسال رسالة، وفتح المحادثة إن لم تكن؛ ويرجع null إن مُنع الإرسال أو خلا النص.</summary>
        Task<MessageThreadView?> SendAsync(
            MessageViewer viewer,
            string otherEmployeeId,
            string body,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// المحادثات على Firestore: وثيقة لكل زوج في <c>threads/{pairKey}</c> تحمل آخر ما قيل
    /// وعدّادي غير المقروء، وسطور الرسائل في مجموعة فرعية تحتها. قائمة المحادثات تقرأ وثائق الأزواج
    /// وحدها بـ array-contains على «participants»، فلا تنزل إلى الرسائل ولا تحتاج فهرساً مركّباً.
    /// </summary>
    public sealed class FirestoreMessageStore(FirestoreDb firestore) : IMessageStore
    {
        /// <summary>سقف نص الرسالة، وهو نفسه سقف الحقل في الوثيقة.</summary>
        public const int MaxBodyLength = 2000;

        /// <summary>أقصى ما تعرضه القائمة: الأحدث بالوقت، فالوثيقة واحدة لكل زوج وعلى أي حال لا تتجاوز العدد.</summary>
        private const int MaxThreadsInList = 200;

        private CollectionReference Threads { get; } = firestore.Collection(AuditCollections.Threads);
        private CollectionReference Users { get; } = firestore.Collection(AuditCollections.Users);
        private CollectionReference Departments { get; } = firestore.Collection(AuditCollections.Departments);

        public async Task<IReadOnlyList<MessageThreadRow>> ListThreadsAsync(
            MessageViewer viewer,
            CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            if (me.Length == 0)
                return [];

            var documents = await Threads
                .WhereArrayContains("participants", me)
                .Limit(MaxThreadsInList)
                .GetSnapshotAsync(cancellationToken);

            var threads = documents.Documents
                .Select(snapshot => snapshot.ConvertTo<MessageThread>()!)
                .Where(thread => thread.OtherOf(me) is not null)
                // الفرز هنا لا في الاستعلام: ترتيب على حقل مع فلتر array-contains فهرسٌ مركّب،
                // والفهارس المركّبة لا تُبنى بلا وحدة تحكم. والمحادثة عند صاحب حساب عشرات لا آلاف.
                .OrderByDescending(thread => thread.LastMessageAt)
                .ThenByDescending(thread => thread.OtherOf(me), StringComparer.Ordinal)
                .ToList();

            var people = await PeopleAsync(
                threads.Select(thread => thread.OtherOf(me)!).Distinct().ToList(), cancellationToken);

            return threads
                .Where(thread => people.ContainsKey(thread.OtherOf(me)!))
                .Select(thread =>
                {
                    var other = people[thread.OtherOf(me)!];
                    return new MessageThreadRow(
                        other.EmployeeId,
                        other.FullName,
                        other.JobTitle,
                        other.DepartmentName,
                        thread.LastBody,
                        thread.LastSenderEmployeeId == me,
                        thread.LastMessageAt,
                        thread.UnreadFor(me));
                })
                .ToList();
        }

        public async Task<int> CountUnreadAsync(string employeeId, CancellationToken cancellationToken = default)
        {
            if (employeeId.Length == 0)
                return 0;

            var documents = await Threads
                .WhereArrayContains("participants", employeeId)
                .GetSnapshotAsync(cancellationToken);

            return documents.Documents
                .Select(snapshot => snapshot.ConvertTo<MessageThread>()!)
                .Sum(thread => thread.UnreadFor(employeeId));
        }

        public async Task<IReadOnlyList<MessageContact>> ListContactsAsync(
            MessageViewer viewer,
            CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            if (me.Length == 0)
                return [];

            // من راسله سابقاً يبقى في القائمة ولو خرج من قواعد الدور، فيستطيع أن يرد عليه.
            var partnerDocuments = await Threads
                .WhereArrayContains("participants", me)
                .GetSnapshotAsync(cancellationToken);

            var partners = partnerDocuments.Documents
                .Select(snapshot => snapshot.ConvertTo<MessageThread>()!.OtherOf(me))
                .Where(other => other is not null)
                .Select(other => other!)
                .ToHashSet(StringComparer.Ordinal);

            var accounts = await Users.GetSnapshotAsync(cancellationToken);
            var names = await DepartmentNamesAsync(
                accounts.Documents
                    .Select(snapshot => snapshot.ConvertTo<User>()!.DepartmentId)
                    .Where(id => id is not null)
                    .Select(id => id!)
                    .ToList(),
                cancellationToken);

            return accounts.Documents
                .Select(snapshot => snapshot.ConvertTo<User>()!)
                .Where(user => user.EmployeeId != me)
                .Where(user => partners.Contains(user.EmployeeId)
                    || CanStart(viewer, user.Role, user.DepartmentId))
                .Select(user => new MessageContact(
                    user.EmployeeId,
                    user.FullName,
                    user.JobTitle,
                    user.DepartmentId,
                    user.DepartmentId is null ? null : names.GetValueOrDefault(user.DepartmentId),
                    user.Role))
                .OrderBy(contact => Rank(contact, viewer.DepartmentId))
                .ThenBy(contact => contact.FullName, StringComparer.Ordinal)
                .ToList();
        }

        public async Task<MessageThreadView?> GetThreadAsync(
            MessageViewer viewer,
            string otherEmployeeId,
            CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            if (me.Length == 0 || otherEmployeeId == me)
                return null;

            var reference = Threads.Document(PairKey(me, otherEmployeeId));
            var snapshot = await reference.GetSnapshotAsync(cancellationToken);
            if (!snapshot.Exists)
                return null;

            var other = await PeopleAsync([otherEmployeeId], cancellationToken);
            if (!other.TryGetValue(otherEmployeeId, out var contact))
                return null;

            var messages = await ReadMessagesAsync(reference, me, cancellationToken);
            return new MessageThreadView(contact, messages);
        }

        public async Task<MessageThreadView?> SendAsync(
            MessageViewer viewer,
            string otherEmployeeId,
            string body,
            CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            var text = body.Trim();

            if (me.Length == 0 || otherEmployeeId == me || text.Length is 0 or > MaxBodyLength)
                return null;

            var recipientSnapshot = await Users.Document(otherEmployeeId).GetSnapshotAsync(cancellationToken);
            if (!recipientSnapshot.Exists)
                return null;

            var recipient = recipientSnapshot.ConvertTo<User>()!;
            var reference = Threads.Document(PairKey(me, otherEmployeeId));
            var now = DateTime.Now;

            var thread = (await reference.GetSnapshotAsync(cancellationToken)).ConvertTo<MessageThread>();

            if (thread is null)
            {
                if (!CanStart(viewer, recipient.Role, recipient.DepartmentId))
                    return null;

                var low = string.CompareOrdinal(me, otherEmployeeId) <= 0 ? me : otherEmployeeId;
                var high = low == me ? otherEmployeeId : me;

                thread = new MessageThread
                {
                    PairKey = reference.Id,
                    EmployeeIdA = low,
                    EmployeeIdB = high,
                    Participants = [low, high],
                    CreatedAt = now,
                    LastMessageAt = now
                };

                try
                {
                    // الإنشاء بشرط «غير موجودة»: محادثة فُتحت في اللحظة نفسها من نافذة أخرى لا تُنسخ،
                    // بل تُقرأ هي كما هي.
                    await reference.CreateAsync(thread, cancellationToken);
                }
                catch (RpcException exception)
                    when (exception.StatusCode is StatusCode.FailedPrecondition or StatusCode.AlreadyExists)
                {
                    thread = (await reference.GetSnapshotAsync(cancellationToken)).ConvertTo<MessageThread>()!;
                }
            }

            thread.LastBody = text;
            thread.LastSenderEmployeeId = me;
            thread.LastMessageAt = now;
            thread.BumpUnread(me);

            var batch = firestore.StartBatch();
            batch.Set(reference, thread, SetOptions.MergeAll);
            batch.Set(reference.Collection(AuditCollections.Messages).Document(), new Message
            {
                SenderEmployeeId = me,
                SenderName = viewer.FullName,
                Body = text,
                SentAt = now
            });
            await batch.CommitAsync(cancellationToken);

            return await GetThreadAsync(viewer, otherEmployeeId, cancellationToken);
        }

        /// <summary>
        /// سطور المحادثة بالترتيب، ثم يُصفَّر عدّاد غير المقروء للقارئ وحده. مفتاح الوثيقة المولّد
        /// مرتّب زمنياً، فترتيب «sentAt» وحده يكفي، ولا يُقرأ شيء من غير ما وصل.
        /// </summary>
        private async Task<IReadOnlyList<MessageRow>> ReadMessagesAsync(
            DocumentReference thread,
            string me,
            CancellationToken cancellationToken)
        {
            var documents = await thread
                .Collection(AuditCollections.Messages)
                .OrderBy("sentAt")
                .GetSnapshotAsync(cancellationToken);

            var rows = documents.Documents
                .Select(snapshot =>
                {
                    var message = snapshot.ConvertTo<Message>()!;
                    return new MessageRow(snapshot.Id, message.SenderEmployeeId == me,
                        message.SenderName, message.Body, message.SentAt);
                })
                .ToList();

            // الكتابة بعد القراءة: لا يمسّ التصفير إلا عدّاد من فتح، فبقاء الآخر شارةً في رقبته.
            var snapshotThread = await thread.GetSnapshotAsync(cancellationToken);
            var state = snapshotThread.ConvertTo<MessageThread>();
            if (state is not null && state.UnreadFor(me) > 0)
            {
                state.ClearUnread(me);
                await snapshotThread.Reference.UpdateAsync(new Dictionary<string, object>
                {
                    ["unreadForA"] = state.UnreadForA,
                    ["unreadForB"] = state.UnreadForB
                }, cancellationToken: cancellationToken);
            }

            return rows;
        }

        /// <summary>بطاقات الحسابات بالأرقام الوظيفية: قراءة مباشرة بمفتاح الوثيقة، بلا استعلام.</summary>
        private async Task<Dictionary<string, MessageContact>> PeopleAsync(
            IReadOnlyList<string> employeeIds,
            CancellationToken cancellationToken)
        {
            if (employeeIds.Count == 0)
                return new Dictionary<string, MessageContact>();

            var names = await DepartmentNamesAsync(
                (await Task.WhenAll(employeeIds.Select(id =>
                    Users.Document(id).GetSnapshotAsync(cancellationToken))))
                .Where(snapshot => snapshot.Exists)
                .Select(snapshot => snapshot.ConvertTo<User>()!.DepartmentId)
                .Where(id => id is not null)
                .Select(id => id!)
                .ToList(),
                cancellationToken);

            var contacts = new Dictionary<string, MessageContact>();

            foreach (var snapshot in await Task.WhenAll(employeeIds.Select(id =>
                         Users.Document(id).GetSnapshotAsync(cancellationToken))))
            {
                if (!snapshot.Exists)
                    continue;

                var user = snapshot.ConvertTo<User>()!;
                contacts[user.EmployeeId] = new MessageContact(
                    user.EmployeeId,
                    user.FullName,
                    user.JobTitle,
                    user.DepartmentId,
                    user.DepartmentId is null ? null : names.GetValueOrDefault(user.DepartmentId),
                    user.Role);
            }

            return contacts;
        }

        /// <summary>أسماء الجهات بالنصوص المحفوظة في وثائقها، لمن يظهر في بطاقة الطرف الآخر.</summary>
        private async Task<Dictionary<string, string>> DepartmentNamesAsync(
            IReadOnlyList<string> departmentIds,
            CancellationToken cancellationToken)
        {
            var distinct = departmentIds.Distinct(StringComparer.Ordinal).ToList();
            if (distinct.Count == 0)
                return new Dictionary<string, string>();

            var snapshots = await Task.WhenAll(distinct.Select(id =>
                Departments.Document(id).GetSnapshotAsync(cancellationToken)));

            return snapshots
                .Where(snapshot => snapshot.Exists)
                .ToDictionary(snapshot => snapshot.Id,
                    snapshot => snapshot.GetValue<string>("departmentName"));
        }

        /// <summary>
        /// قواعد المراسلة: رئيس الوحدة وصاحب النتيجة يراسلان الكل؛ وممثل الجهة ومدقّقها يراسلان
        /// رئيس الوحدة وصاحب النتيجة، وكلٌّ منهما زميله في الجهة نفسها. وما سوى ذلك لا يُفتح.
        /// </summary>
        private static bool CanStart(MessageViewer me, AuditorRole role, string? departmentId) =>
            me.ReachesEveryone
            || role is AuditorRole.QualityHead or AuditorRole.FinalResultReader
            || (me.DepartmentId is not null
                && me.DepartmentId == departmentId
                && ((me.IsRepresentative && role == AuditorRole.EntityAuditor)
                    || (me.IsEntityAuditor && role == AuditorRole.EntityRepresentative)));

        /// <summary>زميل الجهة أولاً، ثم رئيس الوحدة، ثم صاحب النتيجة، ثم المدير، ثم الباقي.</summary>
        private static int Rank(MessageContact contact, string? myDepartmentId) =>
            contact.DepartmentId is not null && contact.DepartmentId == myDepartmentId
                ? 0
                : contact.Role switch
                {
                    AuditorRole.QualityHead => 1,
                    AuditorRole.FinalResultReader => 2,
                    AuditorRole.SystemAdministrator => 3,
                    _ => 4
                };

        private static string PairKey(string a, string b) =>
            string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
    }
}

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
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

    public sealed record MessageRow(int Id, bool IsMine, string SenderName, string Body, DateTime SentAt);

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

    public sealed class EfMessageStore(IDbContextFactory<AppDbContext> contextFactory) : IMessageStore
    {
        /// <summary>سقف نص الرسالة، وهو نفسه سقف العمود في القاعدة.</summary>
        public const int MaxBodyLength = 2000;

        public async Task<IReadOnlyList<MessageThreadRow>> ListThreadsAsync(
            MessageViewer viewer,
            CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            if (me.Length == 0)
                return [];

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            var rows = await db.MessageThreads.AsNoTracking()
                .Where(t => t.EmployeeIdA == me || t.EmployeeIdB == me)
                .OrderByDescending(t => t.LastMessageAt)
                .Select(t => new
                {
                    Other = t.EmployeeIdA == me ? t.EmployeeIdB : t.EmployeeIdA,
                    t.LastMessageAt,
                    LastBody = t.Messages.OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id)
                        .Select(m => m.Body).FirstOrDefault(),
                    LastSender = t.Messages.OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id)
                        .Select(m => m.SenderEmployeeId).FirstOrDefault(),
                    Unread = t.Messages.Count(m => m.SenderEmployeeId != me && m.ReadAt == null)
                })
                .ToListAsync(cancellationToken);

            var people = await PeopleAsync(db, rows.Select(r => r.Other).Distinct().ToList(), cancellationToken);

            return rows
                .Where(r => people.ContainsKey(r.Other))
                .Select(r =>
                {
                    var other = people[r.Other];
                    return new MessageThreadRow(
                        r.Other,
                        other.FullName,
                        other.JobTitle,
                        other.DepartmentName,
                        r.LastBody ?? "",
                        r.LastSender == me,
                        r.LastMessageAt,
                        r.Unread);
                })
                .ToList();
        }

        public async Task<int> CountUnreadAsync(string employeeId, CancellationToken cancellationToken = default)
        {
            if (employeeId.Length == 0)
                return 0;

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await db.Messages.AsNoTracking()
                .Where(m => m.ReadAt == null
                    && m.SenderEmployeeId != employeeId
                    && (m.Thread.EmployeeIdA == employeeId || m.Thread.EmployeeIdB == employeeId))
                .CountAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<MessageContact>> ListContactsAsync(
            MessageViewer viewer,
            CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            if (me.Length == 0)
                return [];

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            // من راسله سابقاً يبقى في القائمة ولو خرج من قواعد الدور، فيستطيع أن يرد عليه.
            var partners = await db.MessageThreads.AsNoTracking()
                .Where(t => t.EmployeeIdA == me || t.EmployeeIdB == me)
                .Select(t => t.EmployeeIdA == me ? t.EmployeeIdB : t.EmployeeIdA)
                .ToListAsync(cancellationToken);

            var users = await db.Users.AsNoTracking()
                .Where(u => u.EmployeeId != me)
                .Select(u => new
                {
                    u.EmployeeId,
                    u.FullName,
                    u.JobTitle,
                    u.Role,
                    u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.DepartmentName : null
                })
                .ToListAsync(cancellationToken);

            return users
                .Where(u => partners.Contains(u.EmployeeId) || CanStart(viewer, u.Role, u.DepartmentId))
                .Select(u => new MessageContact(u.EmployeeId, u.FullName, u.JobTitle, u.DepartmentId, u.DepartmentName, u.Role))
                .OrderBy(c => Rank(c, viewer.DepartmentId))
                .ThenBy(c => c.FullName, StringComparer.Ordinal)
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

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            var key = PairKey(me, otherEmployeeId);
            var threadId = await db.MessageThreads.AsNoTracking()
                .Where(t => t.PairKey == key)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (threadId is null)
                return null;

            var other = await PeopleAsync(db, [otherEmployeeId], cancellationToken);
            if (!other.TryGetValue(otherEmployeeId, out var contact))
                return null;

            var messages = await ReadMessagesAsync(db, threadId.Value, me, cancellationToken);
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

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            var recipient = await db.Users.AsNoTracking()
                .Where(u => u.EmployeeId == otherEmployeeId)
                .Select(u => new { u.EmployeeId, u.Role, u.DepartmentId })
                .FirstOrDefaultAsync(cancellationToken);

            if (recipient is null)
                return null;

            var key = PairKey(me, otherEmployeeId);
            var thread = await db.MessageThreads.FirstOrDefaultAsync(t => t.PairKey == key, cancellationToken);

            if (thread is null)
            {
                if (!CanStart(viewer, recipient.Role, recipient.DepartmentId))
                    return null;

                var now = DateTime.Now;
                var low = string.CompareOrdinal(me, otherEmployeeId) <= 0 ? me : otherEmployeeId;
                var high = low == me ? otherEmployeeId : me;

                thread = new MessageThread
                {
                    PairKey = key,
                    EmployeeIdA = low,
                    EmployeeIdB = high,
                    CreatedAt = now,
                    LastMessageAt = now
                };
                db.MessageThreads.Add(thread);

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // محادثة فُتحت في اللحظة نفسها من نافذة أخرى: تُقرأ هي بدل إنشاء ثانية.
                    db.Entry(thread).State = EntityState.Detached;
                    thread = await db.MessageThreads.FirstAsync(t => t.PairKey == key, cancellationToken);
                }
            }

            thread.LastMessageAt = DateTime.Now;
            db.Messages.Add(new Message
            {
                ThreadId = thread.Id,
                SenderEmployeeId = me,
                SenderName = viewer.FullName,
                Body = text,
                SentAt = thread.LastMessageAt
            });
            await db.SaveChangesAsync(cancellationToken);

            return await GetThreadAsync(viewer, otherEmployeeId, cancellationToken);
        }

        /// <summary>رسائل المحادثة بالترتيب، ثم تُعلَّم رسائل الطرف الآخر مقروءة في القاعدة.</summary>
        private static async Task<IReadOnlyList<MessageRow>> ReadMessagesAsync(
            AppDbContext db,
            int threadId,
            string me,
            CancellationToken cancellationToken)
        {
            var messages = await db.Messages.AsNoTracking()
                .Where(m => m.ThreadId == threadId)
                .OrderBy(m => m.SentAt).ThenBy(m => m.Id)
                .Select(m => new { m.Id, m.SenderEmployeeId, m.SenderName, m.Body, m.SentAt })
                .ToListAsync(cancellationToken);

            var readAt = DateTime.Now;
            await db.Messages
                .Where(m => m.ThreadId == threadId && m.SenderEmployeeId != me && m.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAt, readAt), cancellationToken);

            return messages
                .Select(m => new MessageRow(m.Id, m.SenderEmployeeId == me, m.SenderName, m.Body, m.SentAt))
                .ToList();
        }

        private static async Task<Dictionary<string, MessageContact>> PeopleAsync(
            AppDbContext db,
            IReadOnlyList<string> employeeIds,
            CancellationToken cancellationToken)
        {
            if (employeeIds.Count == 0)
                return new Dictionary<string, MessageContact>();

            var people = await db.Users.AsNoTracking()
                .Where(u => employeeIds.Contains(u.EmployeeId))
                .Select(u => new MessageContact(
                    u.EmployeeId,
                    u.FullName,
                    u.JobTitle,
                    u.DepartmentId,
                    u.Department != null ? u.Department.DepartmentName : null,
                    u.Role))
                .ToListAsync(cancellationToken);

            return people.ToDictionary(p => p.EmployeeId);
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

using Microsoft.Data.Sqlite;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// المحادثات على القاعدة المحلية: صفٌّ لكل زوج يحمل آخر ما قيل وعدّادي غير المقروء، وسطور
    /// الرسائل في جدولها. قائمة المحادثات تقرأ الأزواج وحدها بفلترٍ على أحد الطرفين، وبطاقات
    /// الحسابات تُقرأ مرة واحدة في الطلب نفسه فلا يُجلب الصف ذاته مرتين.
    /// </summary>
    public sealed class SqliteMessageStore(LocalDb db) : IMessageStore
    {
        /// <summary>سقف نص الرسالة.</summary>
        public const int MaxBodyLength = 2000;

        /// <summary>أقصى ما تعرضه القائمة: الأحدث بالوقت.</summary>
        private const int MaxThreadsInList = 200;

        public async Task<IReadOnlyList<MessageThreadRow>> ListThreadsAsync(
            MessageViewer viewer, CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            if (me.Length == 0)
                return [];

            var threads = await db.QueryAsync(
                """
                select * from threads
                where employee_id_a = @me or employee_id_b = @me
                order by last_message_at desc
                limit @limit
                """,
                MapThread, cancellationToken, ("@me", me), ("@limit", MaxThreadsInList));

            var people = await PeopleAsync(cancellationToken);

            return threads
                .Where(thread => thread.OtherOf(me) is not null && people.ContainsKey(thread.OtherOf(me)!))
                .OrderByDescending(thread => thread.LastMessageAt)
                .ThenByDescending(thread => thread.OtherOf(me), StringComparer.Ordinal)
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

            var total = await db.ScalarAsync(
                """
                select coalesce(sum(case when employee_id_a = @me then unread_for_a else unread_for_b end), 0)
                from threads
                where employee_id_a = @me or employee_id_b = @me
                """,
                cancellationToken, ("@me", employeeId));

            return Convert.ToInt32(total);
        }

        public async Task<IReadOnlyList<MessageContact>> ListContactsAsync(
            MessageViewer viewer, CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            if (me.Length == 0)
                return [];

            // من راسله سابقاً يبقى في القائمة ولو خرج من قواعد الدور، فيستطيع أن يرد عليه.
            var partners = (await db.QueryAsync(
                """
                select case when employee_id_a = @me then employee_id_b else employee_id_a end
                from threads
                where employee_id_a = @me or employee_id_b = @me
                """,
                reader => reader.GetString(0), cancellationToken, ("@me", me)))
                .ToHashSet(StringComparer.Ordinal);

            var people = await PeopleAsync(cancellationToken);

            return people.Values
                .Where(user => user.EmployeeId != me)
                .Where(user => partners.Contains(user.EmployeeId)
                    || CanStart(viewer, user.Role))
                .OrderBy(contact => Rank(contact, viewer.DepartmentId))
                .ThenBy(contact => contact.FullName, StringComparer.Ordinal)
                .ToList();
        }

        public async Task<MessageThreadView?> GetThreadAsync(
            MessageViewer viewer, string otherEmployeeId, CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            if (me.Length == 0 || otherEmployeeId == me)
                return null;

            var pairKey = PairKey(me, otherEmployeeId);
            var thread = await db.FirstAsync(
                "select * from threads where pair_key = @pairKey", MapThread, cancellationToken,
                ("@pairKey", pairKey));

            if (thread is null)
                return null;

            var people = await PeopleAsync(cancellationToken);
            if (!people.TryGetValue(otherEmployeeId, out var contact))
                return null;

            var messages = await ReadMessagesAsync(pairKey, me, cancellationToken);
            return new MessageThreadView(contact, messages);
        }

        public async Task<MessageThreadView?> SendAsync(
            MessageViewer viewer, string otherEmployeeId, string body, CancellationToken cancellationToken = default)
        {
            var me = viewer.EmployeeId;
            var text = body.Trim();

            if (me.Length == 0 || otherEmployeeId == me || text.Length is 0 or > MaxBodyLength)
                return null;

            var people = await PeopleAsync(cancellationToken);
            if (!people.TryGetValue(otherEmployeeId, out var recipient))
                return null;

            var pairKey = PairKey(me, otherEmployeeId);
            var now = DateTime.Now;

            var exists = await db.ScalarAsync(
                "select 1 from threads where pair_key = @pairKey", cancellationToken,
                ("@pairKey", pairKey)) is not null;

            if (!exists && !CanStart(viewer, recipient.Role))
                return null;

            var low = string.CompareOrdinal(me, otherEmployeeId) <= 0 ? me : otherEmployeeId;
            var high = low == me ? otherEmployeeId : me;
            var messageId = Guid.NewGuid().ToString("N");
            var lastAt = LocalDb.Moment(now);

            await db.InTransactionAsync(async (connection, transaction, token) =>
            {
                // الإنشاء بشرط «غير موجود»: محادثة فُتحت في اللحظة نفسها من نافذة أخرى لا تُنسخ.
                await using (var command = LocalDb.Build(connection,
                    """
                    insert or ignore into threads
                        (pair_key, employee_id_a, employee_id_b, created_at, last_message_at,
                         last_body, last_sender_employee_id, unread_for_a, unread_for_b)
                    values (@pairKey, @a, @b, @createdAt, @createdAt, '', '', 0, 0)
                    """,
                    [
                        ("@pairKey", pairKey),
                        ("@a", low),
                        ("@b", high),
                        ("@createdAt", lastAt)
                    ], transaction))
                {
                    await command.ExecuteNonQueryAsync(token);
                }

                // الزيادة على العدّاد نفسه لا قيمةً محسوبة قبل القراءة: نافذتان ترسلان في اللحظة
                // ذاتها فتُحسب رسالتان لا واحدة.
                await using (var command = LocalDb.Build(connection,
                    """
                    update threads
                    set last_message_at = @lastAt, last_body = @lastBody,
                        last_sender_employee_id = @me,
                        unread_for_a = case when employee_id_a = @me then unread_for_a else unread_for_a + 1 end,
                        unread_for_b = case when employee_id_b = @me then unread_for_b else unread_for_b + 1 end
                    where pair_key = @pairKey
                    """,
                    [
                        ("@lastAt", lastAt),
                        ("@lastBody", text),
                        ("@me", me),
                        ("@pairKey", pairKey)
                    ], transaction))
                {
                    await command.ExecuteNonQueryAsync(token);
                }

                await using (var command = LocalDb.Build(connection,
                    """
                    insert into messages (id, thread_pair_key, sender_employee_id, sender_name, body, sent_at)
                    values (@id, @pairKey, @sender, @senderName, @body, @sentAt)
                    """,
                    [
                        ("@id", messageId),
                        ("@pairKey", pairKey),
                        ("@sender", me),
                        ("@senderName", viewer.FullName),
                        ("@body", text),
                        ("@sentAt", LocalDb.Moment(now))
                    ], transaction))
                {
                    await command.ExecuteNonQueryAsync(token);
                }
            }, cancellationToken);

            return await GetThreadAsync(viewer, otherEmployeeId, cancellationToken);
        }

        /// <summary>
        /// سطور المحادثة بالترتيب، ثم يُصفَّر عدّاد غير المقروء للقارئ وحده: لا يمسّ التصفير إلا
        /// عدّاد من فتح، فبقاء الآخر شارةً في رقبته.
        /// </summary>
        private async Task<IReadOnlyList<MessageRow>> ReadMessagesAsync(
            string pairKey, string me, CancellationToken cancellationToken)
        {
            var rows = await db.QueryAsync(
                """
                select id, sender_employee_id, sender_name, body, sent_at
                from messages
                where thread_pair_key = @pairKey
                order by sent_at, id
                """,
                reader => new MessageRow(
                    reader.GetString(0),
                    reader.GetString(1) == me,
                    reader.GetString(2),
                    reader.GetString(3),
                    LocalDb.ReadMoment(reader.GetString(4))),
                cancellationToken, ("@pairKey", pairKey));

            await db.ExecuteAsync(
                """
                update threads
                set unread_for_a = case when employee_id_a = @me then 0 else unread_for_a end,
                    unread_for_b = case when employee_id_b = @me then 0 else unread_for_b end
                where pair_key = @pairKey
                """,
                cancellationToken, ("@me", me), ("@pairKey", pairKey));

            return rows;
        }

        /// <summary>بطاقات الحسابات كلها بأسماء جهاتها: قراءة واحدة في الطلب، لا جلبٌ متكرّر للصف نفسه.</summary>
        private async Task<Dictionary<string, MessageContact>> PeopleAsync(CancellationToken cancellationToken)
        {
            var rows = await db.QueryAsync(
                """
                select u.employee_id, u.full_name, u.job_title, u.department_id, u.role, d.department_name
                from users u
                left join departments d on d.department_id = u.department_id
                """,
                reader => new MessageContact(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    LocalDb.Text(reader, "department_id"),
                    LocalDb.Text(reader, "department_name"),
                    Enum.Parse<AuditorRole>(reader.GetString(4))),
                cancellationToken);

            return rows.ToDictionary(contact => contact.EmployeeId, StringComparer.Ordinal);
        }

        private static MessageThread MapThread(SqliteDataReader reader) => new()
        {
            PairKey = reader.GetString(reader.GetOrdinal("pair_key")),
            EmployeeIdA = reader.GetString(reader.GetOrdinal("employee_id_a")),
            EmployeeIdB = reader.GetString(reader.GetOrdinal("employee_id_b")),
            Participants =
            [
                reader.GetString(reader.GetOrdinal("employee_id_a")),
                reader.GetString(reader.GetOrdinal("employee_id_b"))
            ],
            CreatedAt = LocalDb.ReadMoment(reader.GetString(reader.GetOrdinal("created_at"))),
            LastMessageAt = LocalDb.ReadMoment(reader.GetString(reader.GetOrdinal("last_message_at"))),
            LastBody = reader.GetString(reader.GetOrdinal("last_body")),
            LastSenderEmployeeId = reader.GetString(reader.GetOrdinal("last_sender_employee_id")),
            UnreadForA = reader.GetInt32(reader.GetOrdinal("unread_for_a")),
            UnreadForB = reader.GetInt32(reader.GetOrdinal("unread_for_b"))
        };

        /// <summary>
        /// قواعد المراسلة: رئيس الوحدة وصاحب النتيجة والمدير يراسلون الكل؛ وممثل الجهة يراسل
        /// رئيس الوحدة وصاحب النتيجة. وما سوى ذلك لا يُفتح.
        /// </summary>
        private static bool CanStart(MessageViewer me, AuditorRole role) =>
            me.ReachesEveryone
            || role is AuditorRole.QualityHead or AuditorRole.FinalResultReader;

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
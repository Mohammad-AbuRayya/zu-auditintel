using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// سجل الإشعارات على القاعدة المحلية. دوائر الرؤية جدولٌ مفهرس لا مصفوفة داخل الوثيقة، ففلتر
    /// الجهة وعدّاد غير المقروء وتعليم القراءة استعلاماتٌ على فهارس: لا مسح للسجل ولا سقفٌ على
    /// عدد الكتابات، وتعليم القراءة كله عبارة واحدة مهما طال السجل.
    /// </summary>
    public sealed class SqliteNotificationStore(LocalDb db, ILogger<SqliteNotificationStore> logger) : INotificationStore
    {
        /// <summary>سقف ما تُعرضه الصفحة؛ السجل يطول مع رفع كل ملف، والصفحة للجديد لا لأرشيف كامل.</summary>
        public const int MaxRows = 200;

        public async Task<IReadOnlyList<NotificationRow>> ListAsync(
            NotificationViewer viewer, CancellationToken cancellationToken = default)
        {
            var (filter, args) = Visibility(viewer);
            if (filter is null)
                return [];

            var rows = await db.QueryAsync(
                $"""
                select n.*, r.employee_id is null as is_new
                from notifications n
                {filter}
                left join notification_read r on r.notification_id = n.id and r.employee_id = @me
                order by n.created_at desc, n.id desc
                limit @limit
                """,
                reader => new NotificationRow(
                    reader.GetString(reader.GetOrdinal("id")),
                    Enum.Parse<NotificationKind>(reader.GetString(reader.GetOrdinal("kind"))),
                    reader.GetString(reader.GetOrdinal("department_id")),
                    reader.GetString(reader.GetOrdinal("department_name")),
                    reader.GetString(reader.GetOrdinal("item_title")),
                    LocalDb.Text(reader, "file_name"),
                    LocalDb.Text(reader, "actor_name"),
                    LocalDb.Int(reader, "agent_score"),
                    LocalDb.Text(reader, "note"),
                    LocalDb.ReadMoment(reader.GetString(reader.GetOrdinal("created_at"))),
                    reader.GetInt32(reader.GetOrdinal("is_new")) != 0),
                cancellationToken,
                [.. args, ("@me", viewer.EmployeeId), ("@limit", MaxRows)]);

            return rows;
        }

        public async Task<int> CountUnreadAsync(
            NotificationViewer viewer, CancellationToken cancellationToken = default)
        {
            if (viewer.EmployeeId.Length == 0)
                return 0;

            var (filter, args) = Visibility(viewer);
            if (filter is null)
                return 0;

            var count = await db.ScalarAsync(
                $"""
                select count(*)
                from notifications n
                {filter}
                left join notification_read r on r.notification_id = n.id and r.employee_id = @me
                where r.employee_id is null
                """,
                cancellationToken,
                [.. args, ("@me", viewer.EmployeeId)]);

            return Convert.ToInt32(count);
        }

        public async Task MarkAllReadAsync(
            NotificationViewer viewer, CancellationToken cancellationToken = default)
        {
            if (viewer.EmployeeId.Length == 0)
                return;

            var (filter, args) = Visibility(viewer);
            if (filter is null)
                return;

            // سطر السجل أثرٌ لا قرار: سقوطه لا يُسقط ما قرأه صاحب الشاشة، فيُرفض بهدوء ويُسجَّل تحذيره.
            try
            {
                await db.ExecuteAsync(
                    $"""
                    insert or ignore into notification_read (notification_id, employee_id)
                    select n.id, @me
                    from notifications n
                    {filter}
                    where not exists (
                        select 1 from notification_read r
                        where r.notification_id = n.id and r.employee_id = @me)
                    """,
                    cancellationToken,
                    [.. args, ("@me", viewer.EmployeeId)]);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "تعذّر تعليم الإشعارات مقروءة.");
            }
        }

        public async Task RecordAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            notification.VisibleTo = Audience(notification.Kind, notification.DepartmentId);
            notification.ReadBy = [];

            if (notification.Id.Length == 0)
                notification.Id = Guid.NewGuid().ToString("N");

            try
            {
                await db.InTransactionAsync(async (connection, transaction, token) =>
                {
                    await using (var command = LocalDb.Build(connection,
                        """
                        insert or replace into notifications
                            (id, kind, department_id, department_name, item_title, file_name,
                             actor_name, agent_score, note, created_at)
                        values (@id, @kind, @departmentId, @departmentName, @itemTitle, @fileName,
                                @actorName, @agentScore, @note, @createdAt)
                        """,
                        [
                            ("@id", notification.Id),
                            ("@kind", notification.Kind.ToString()),
                            ("@departmentId", notification.DepartmentId),
                            ("@departmentName", notification.DepartmentName),
                            ("@itemTitle", notification.ItemTitle),
                            ("@fileName", notification.FileName),
                            ("@actorName", notification.ActorName),
                            ("@agentScore", notification.AgentScore),
                            ("@note", notification.Note),
                            ("@createdAt", LocalDb.Moment(notification.CreatedAt))
                        ],
                        transaction))
                    {
                        await command.ExecuteNonQueryAsync(token);
                    }

                    foreach (var circle in notification.VisibleTo)
                    {
                        await using var command = LocalDb.Build(connection,
                            """
                            insert or ignore into notification_audience (circle, notification_id)
                            values (@circle, @id)
                            """,
                            [("@circle", circle), ("@id", notification.Id)], transaction);

                        await command.ExecuteNonQueryAsync(token);
                    }
                }, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "تعذّر كتابة سطر إشعار.");
            }
        }

        /// <summary>
        /// دوائر من يحق له رؤية هذا السطر. جهةُ الحدث دائرته دائماً، و«kind:Approved» دائرة صاحب
        /// النتيجة وحده، فهي الاستثناء الوحيد الذي يقرأ من خارج جهته: ما أُغلق نهائياً على مستوى
        /// الجامعة. ولا دائرة عامة تُكتب، فرئيس الوحدة ومدير النظام يقرآن السجل كله بلا شرط.
        /// </summary>
        private static List<string> Audience(NotificationKind kind, string departmentId)
        {
            var audience = new List<string> { $"dept:{departmentId}" };
            if (kind == NotificationKind.Approved)
                audience.Add("kind:Approved");

            return audience;
        }

        /// <summary>شرط الرؤية ووسمه: null يعني لا يرى هذا الحساب شيئاً بدل أن يرى إشعارات جهة ليست جهته.</summary>
        private static (string? Filter, (string, object?)[] Args) Visibility(NotificationViewer viewer)
        {
            if (viewer.SeesAllDepartments)
                return ("", []);

            if (viewer.DepartmentId is not null)
                return (
                    "join notification_audience a on a.notification_id = n.id and a.circle = @circle",
                    [("@circle", $"dept:{viewer.DepartmentId}")]);

            if (viewer.ResultsOnly)
                return (
                    "join notification_audience a on a.notification_id = n.id and a.circle = @circle",
                    [("@circle", "kind:Approved")]);

            return (null, []);
        }
    }
}
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
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
        int Id,
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
    }

    public sealed class EfNotificationStore(IDbContextFactory<AppDbContext> contextFactory) : INotificationStore
    {
        /// <summary>سقف ما تُعرضه الصفحة؛ السجل يطول مع رفع كل ملف، والصفحة للجديد لا لأرشيف كامل.</summary>
        public const int MaxRows = 200;

        public async Task<IReadOnlyList<NotificationRow>> ListAsync(
            NotificationViewer viewer,
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var employeeId = viewer.EmployeeId;

            return await Visible(db, viewer)
                .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
                .Take(MaxRows)
                .Select(n => new NotificationRow(
                    n.Id,
                    n.Kind,
                    n.DepartmentId,
                    n.DepartmentName,
                    n.ItemTitle,
                    n.FileName,
                    n.ActorName,
                    n.AgentScore,
                    n.Note,
                    n.CreatedAt,
                    !db.NotificationReads.Any(r => r.NotificationId == n.Id && r.EmployeeId == employeeId)))
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountUnreadAsync(
            NotificationViewer viewer,
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var employeeId = viewer.EmployeeId;

            return await Visible(db, viewer)
                .Where(n => !db.NotificationReads.Any(r => r.NotificationId == n.Id && r.EmployeeId == employeeId))
                .CountAsync(cancellationToken);
        }

        public async Task MarkAllReadAsync(
            NotificationViewer viewer,
            CancellationToken cancellationToken = default)
        {
            if (viewer.EmployeeId.Length == 0)
                return;

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var employeeId = viewer.EmployeeId;

            var unread = await Visible(db, viewer)
                .Where(n => !db.NotificationReads.Any(r => r.NotificationId == n.Id && r.EmployeeId == employeeId))
                .Select(n => n.Id)
                .ToListAsync(cancellationToken);

            if (unread.Count == 0)
                return;

            var readAt = DateTime.Now;
            db.NotificationReads.AddRange(unread.Select(id => new NotificationRead
            {
                NotificationId = id,
                EmployeeId = employeeId,
                ReadAt = readAt
            }));
            await db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// ما يحق لهذا الحساب رؤيته. من لا جهة له ولا صفة تفتح السجل كله لا يرى شيئاً بدل أن يرى
        /// إشعارات جهة ليست جهته؛ وصاحب النتيجة استثناء واحد: يقرأ ما أُغلق نهائياً فقط.
        /// </summary>
        private static IQueryable<Notification> Visible(AppDbContext db, NotificationViewer viewer)
        {
            var query = db.Notifications.AsNoTracking();

            if (viewer.SeesAllDepartments)
                return query;

            var departmentId = viewer.DepartmentId;
            if (departmentId is not null)
                return query.Where(n => n.DepartmentId == departmentId);

            return viewer.ResultsOnly
                ? query.Where(n => n.Kind == NotificationKind.Approved)
                : query.Where(n => false);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>تعريف بند تدقيق كما يُسلَّم إلى الواجهة: بيانات الكتالوج فقط، بلا حالة تشغيل.</summary>
    public sealed record AuditItemDefinition(
        int Id,
        int Number,
        string Title,
        string IsoReference,
        string Workflow,
        AuditItemCategory Category,
        IReadOnlyList<string> Questions,
        IReadOnlyList<string> RequiredEvidence);

    /// <summary>الجهة كما تحتاجها واجهة التدقيق: المعرّف والاسم المعروض.</summary>
    public sealed record AuditDepartment(string DepartmentId, string DepartmentName);

    /// <summary>
    /// بندٌ في فهرس البحث: جهته ورقمه داخلها وعنوانه ورمز آيزو، بلا أسئلته وأدلته. شاشة التدقيق
    /// النهائي تبحث بالبند أو الرمز فتعرف أي جهة تُفتح، دون أن تحمّل لوحة كل جهة لتعرف ذلك.
    /// </summary>
    public sealed record AuditItemIndexEntry(string DepartmentId, int Number, string Title, string IsoReference);

    public interface IAuditCatalog
    {
        Task<IReadOnlyList<AuditItemDefinition>> GetItemsAsync(
            string departmentId,
            CancellationToken cancellationToken = default);

        /// <summary>فهرس بنود كل الجهات بأرقامها، بالترتيب نفسه الذي تُعرض به لوحة الجهة.</summary>
        Task<IReadOnlyList<AuditItemIndexEntry>> GetItemIndexAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AuditDepartment>> GetDepartmentsAsync(
            CancellationToken cancellationToken = default);

        Task<AuditDepartment?> GetDepartmentAsync(
            string departmentId,
            CancellationToken cancellationToken = default);
    }

    public sealed class EfAuditCatalog(IDbContextFactory<AppDbContext> contextFactory) : IAuditCatalog
    {
        public async Task<IReadOnlyList<AuditItemDefinition>> GetItemsAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            var rows = await db.AuditItems.AsNoTracking()
                .Where(i => i.DepartmentId == departmentId)
                .OrderBy(i => i.SortOrder)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    i.IsoReference,
                    i.Workflow,
                    i.Category,
                    Questions = i.Questions.OrderBy(q => q.SortOrder).Select(q => q.Text).ToList(),
                    Evidence = i.EvidenceRequirements.OrderBy(e => e.SortOrder).Select(e => e.Text).ToList()
                })
                .ToListAsync(cancellationToken);

            return rows
                .Select((r, index) => new AuditItemDefinition(
                    r.Id, index + 1, r.Title, r.IsoReference, r.Workflow, r.Category, r.Questions, r.Evidence))
                .ToList();
        }

        public async Task<IReadOnlyList<AuditItemIndexEntry>> GetItemIndexAsync(
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            var rows = await db.AuditItems.AsNoTracking()
                .OrderBy(i => i.DepartmentId).ThenBy(i => i.SortOrder)
                .Select(i => new { i.DepartmentId, i.Title, i.IsoReference })
                .ToListAsync(cancellationToken);

            // الرقم ترتيبُ البند داخل جهته لا معرّفه، وهو نفسه الذي يُحسب في GetItemsAsync.
            var index = new List<AuditItemIndexEntry>(rows.Count);
            string? currentDepartment = null;
            var number = 0;
            foreach (var row in rows)
            {
                number = row.DepartmentId == currentDepartment ? number + 1 : 1;
                currentDepartment = row.DepartmentId;
                index.Add(new AuditItemIndexEntry(row.DepartmentId, number, row.Title, row.IsoReference));
            }

            return index;
        }

        public async Task<IReadOnlyList<AuditDepartment>> GetDepartmentsAsync(
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await db.Departments.AsNoTracking()
                .OrderBy(d => d.DepartmentName)
                .Select(d => new AuditDepartment(d.DepartmentId, d.DepartmentName))
                .ToListAsync(cancellationToken);
        }
        public async Task<AuditDepartment?> GetDepartmentAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await db.Departments.AsNoTracking()
                .Where(d => d.DepartmentId == departmentId)
                .Select(d => new AuditDepartment(d.DepartmentId, d.DepartmentName))
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}

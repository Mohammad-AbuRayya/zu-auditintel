using Microsoft.EntityFrameworkCore;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>تعريف بند تدقيق كما يُسلَّم إلى الواجهة: بيانات الكتالوج فقط، بلا حالة تشغيل.</summary>
    public sealed record AuditItemDefinition(
        int Id,
        string Title,
        AuditItemCategory Category,
        IReadOnlyList<string> Questions,
        IReadOnlyList<string> RequiredEvidence);

    public interface IAuditCatalog
    {
        Task<IReadOnlyList<AuditItemDefinition>> GetItemsAsync(
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
                    i.Category,
                    Questions = i.Questions.OrderBy(q => q.SortOrder).Select(q => q.Text).ToList(),
                    Evidence = i.EvidenceRequirements.OrderBy(e => e.SortOrder).Select(e => e.Text).ToList()
                })
                .ToListAsync(cancellationToken);

            return rows
                .Select(r => new AuditItemDefinition(r.Id, r.Title, r.Category, r.Questions, r.Evidence))
                .ToList();
        }
    }
}

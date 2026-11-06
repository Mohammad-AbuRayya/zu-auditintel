using Google.Cloud.Firestore;
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

    /// <summary>
    /// الكتالوج على Firestore: مجموعة وثائق «items» و«departments» تُقرأ مرة واحدة في عمر العملية
    /// وتُخبَّ في نسخة واحدة لا تُبدَّل. الثوابت لا تتغيّر إلا بإعادة تصدير، فكل لوحة بعده تقرأ
    /// من الذاكرة، ولا يستهلك عرضُ بندٍ قراءةً من السحابة.
    /// </summary>
    public sealed class FirestoreAuditCatalog(FirestoreDb firestore) : IAuditCatalog
    {
        private sealed record Snapshot(
            IReadOnlyDictionary<string, IReadOnlyList<AuditItemDefinition>> ItemsByDepartment,
            IReadOnlyList<AuditItemIndexEntry> Index,
            IReadOnlyList<AuditDepartment> Departments,
            IReadOnlyDictionary<string, AuditDepartment> DepartmentsById);

        private readonly SemaphoreSlim loadGate = new(1, 1);
        private volatile Snapshot? snapshot;

        public async Task<IReadOnlyList<AuditItemDefinition>> GetItemsAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            var current = await LoadAsync(cancellationToken);
            return current.ItemsByDepartment.GetValueOrDefault(departmentId, []);
        }

        public async Task<IReadOnlyList<AuditItemIndexEntry>> GetItemIndexAsync(
            CancellationToken cancellationToken = default) =>
            (await LoadAsync(cancellationToken)).Index;

        public async Task<IReadOnlyList<AuditDepartment>> GetDepartmentsAsync(
            CancellationToken cancellationToken = default) =>
            (await LoadAsync(cancellationToken)).Departments;

        public async Task<AuditDepartment?> GetDepartmentAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            var current = await LoadAsync(cancellationToken);
            return current.DepartmentsById.GetValueOrDefault(departmentId);
        }

        private async Task<Snapshot> LoadAsync(CancellationToken cancellationToken)
        {
            if (snapshot is { } cached)
                return cached;

            await loadGate.WaitAsync(cancellationToken);
            try
            {
                if (snapshot is { } twice)
                    return twice;

                var documents = await firestore.Collection(AuditCollections.Items).GetSnapshotAsync(cancellationToken);
                var departments = await firestore.Collection(AuditCollections.Departments)
                    .GetSnapshotAsync(cancellationToken);

                var byDepartment = documents.Documents
                    .Select(document => document.ConvertTo<AuditItem>()!)
                    .GroupBy(item => item.DepartmentId)
                    .ToDictionary(
                        group => group.Key,
                        group => (IReadOnlyList<AuditItemDefinition>)group
                            .OrderBy(item => item.SortOrder).ThenBy(item => item.Id)
                            .Select((item, index) => new AuditItemDefinition(
                                item.Id,
                                index + 1,
                                item.Title,
                                item.IsoReference,
                                item.Workflow,
                                item.Category,
                                item.Questions,
                                item.RequiredEvidence))
                            .ToList());

                // الرقم ترتيبُ البند داخل جهته لا معرّفه، وهو نفسه الذي تُعرض به اللوحة.
                var index = byDepartment
                    .SelectMany(pair => pair.Value
                        .OrderBy(definition => definition.Number)
                        .Select(definition => new AuditItemIndexEntry(
                            pair.Key, definition.Number, definition.Title, definition.IsoReference)))
                    .OrderBy(entry => entry.DepartmentId, StringComparer.Ordinal)
                    .ThenBy(entry => entry.Number)
                    .ToList();

                var names = departments.Documents
                    .Select(document => new AuditDepartment(
                        document.Id,
                        document.GetValue<string>("departmentName")))
                    .OrderBy(department => department.DepartmentName, StringComparer.Ordinal)
                    .ToList();

                snapshot = new Snapshot(
                    byDepartment,
                    index,
                    names,
                    names.ToDictionary(department => department.DepartmentId));

                return snapshot;
            }
            finally
            {
                loadGate.Release();
            }
        }
    }
}

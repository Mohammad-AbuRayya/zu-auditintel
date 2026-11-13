using System.Text.Json;
using Microsoft.Data.Sqlite;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// الكتالوج من القاعدة المحلية: البنود والجهات تُقرأ مرة واحدة في عمر العملية وتُخبَّ في نسخة
    /// واحدة لا تُبدَّل. الثوابت لا تتغيّر إلا بإعادة استيراد، فكل لوحة بعده تقرأ من الذاكرة.
    /// والترقيم والإسقاط هما هما في كل مصدر: رقم البند ترتيبه داخل جهته لا معرّفه.
    /// </summary>
    public sealed class SqliteAuditCatalog(LocalDb db) : IAuditCatalog
    {
        private sealed record Snapshot(
            IReadOnlyDictionary<string, IReadOnlyList<AuditItemDefinition>> ItemsByDepartment,
            IReadOnlyList<AuditItemIndexEntry> Index,
            IReadOnlyList<AuditDepartment> Departments,
            IReadOnlyDictionary<string, AuditDepartment> DepartmentsById);

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly SemaphoreSlim loadGate = new(1, 1);
        private volatile Snapshot? snapshot;

        public async Task<IReadOnlyList<AuditItemDefinition>> GetItemsAsync(
            string departmentId, CancellationToken cancellationToken = default) =>
            (await LoadAsync(cancellationToken)).ItemsByDepartment.GetValueOrDefault(departmentId, []);

        public async Task<IReadOnlyList<AuditItemIndexEntry>> GetItemIndexAsync(
            CancellationToken cancellationToken = default) =>
            (await LoadAsync(cancellationToken)).Index;

        public async Task<IReadOnlyList<AuditDepartment>> GetDepartmentsAsync(
            CancellationToken cancellationToken = default) =>
            (await LoadAsync(cancellationToken)).Departments;

        public async Task<AuditDepartment?> GetDepartmentAsync(
            string departmentId, CancellationToken cancellationToken = default) =>
            (await LoadAsync(cancellationToken)).DepartmentsById.GetValueOrDefault(departmentId);

        private async Task<Snapshot> LoadAsync(CancellationToken cancellationToken)
        {
            if (snapshot is { } cached)
                return cached;

            await loadGate.WaitAsync(cancellationToken);
            try
            {
                if (snapshot is { } twice)
                    return twice;

                var items = await db.QueryAsync(
                    "select * from audit_items", MapItem, cancellationToken);

                var departments = await db.QueryAsync(
                    "select department_id, department_name from departments",
                    reader => new AuditDepartment(reader.GetString(0), reader.GetString(1)),
                    cancellationToken);

                snapshot = Build(items, departments);
                return snapshot;
            }
            finally
            {
                loadGate.Release();
            }
        }

        internal static AuditItem MapItem(SqliteDataReader reader) => new()
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            DepartmentId = reader.GetString(reader.GetOrdinal("department_id")),
            Category = Enum.Parse<AuditItemCategory>(reader.GetString(reader.GetOrdinal("category"))),
            Title = reader.GetString(reader.GetOrdinal("title")),
            IsoReference = reader.GetString(reader.GetOrdinal("iso_reference")),
            Workflow = reader.GetString(reader.GetOrdinal("workflow")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
            RequiredEvidence = Lines(LocalDb.Text(reader, "required_evidence")),
            Questions = Lines(LocalDb.Text(reader, "questions")),
            Sheet = new AuditItemSheet
            {
                InstructionsEvidence = LocalDb.Text(reader, "sheet_instructions_evidence") ?? "",
                InternalAuditEvidence = LocalDb.Text(reader, "sheet_internal_audit_evidence") ?? "",
                InstructionsWorkflow = LocalDb.Text(reader, "sheet_instructions_workflow") ?? "",
                InstructionsQuestions = LocalDb.Text(reader, "sheet_instructions_questions") ?? ""
            }
        };

        internal static List<string> Lines(string? json) =>
            string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<string>>(json, Json) ?? [];

        internal static string ToJson(IReadOnlyList<string> lines) => JsonSerializer.Serialize(lines, Json);

        private static Snapshot Build(IEnumerable<AuditItem> items, IEnumerable<AuditDepartment> departments)
        {
            var byDepartment = items
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

            var index = byDepartment
                .SelectMany(pair => pair.Value
                    .OrderBy(definition => definition.Number)
                    .Select(definition => new AuditItemIndexEntry(
                        pair.Key, definition.Number, definition.Title, definition.IsoReference)))
                .OrderBy(entry => entry.DepartmentId, StringComparer.Ordinal)
                .ThenBy(entry => entry.Number)
                .ToList();

            var names = departments
                .OrderBy(department => department.DepartmentName, StringComparer.Ordinal)
                .ToList();

            return new Snapshot(
                byDepartment,
                index,
                names,
                names.ToDictionary(department => department.DepartmentId));
        }
    }
}
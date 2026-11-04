using Microsoft.EntityFrameworkCore;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>بوابة التدقيق الآلي: نسبة العيب تُحسب من درجة البند، و10% فأقل تعبر إلى التدقيق البشري.</summary>
    public static class AgentGate
    {
        public const int MaxDefectRatePercent = 10;

        public static bool IsApproved(int score) => 100 - score <= MaxDefectRatePercent;

        public static string VerdictLabel(AgentOutcome outcome) => outcome switch
        {
            AgentOutcome.Approved => "مقبول أولياً – بانتظار التدقيق البشري",
            AgentOutcome.Rejected => "مرفوض – يتطلب إجراءً تصحيحياً",
            _ => "بانتظار الفحص الآلي"
        };
    }

    /// <summary>ملف جديد كما يسلّمه نموذج الرفع في لوحة الجهة.</summary>
    public sealed record NewEvidenceUpload(
        string DepartmentId,
        int AuditItemId,
        string FileName,
        string Extension,
        byte[] Bytes,
        string UploaderEmployeeId,
        string UploaderName);

    /// <summary>
    /// حالة بند الجهة على لوحته، وهي آلة واحدة تتقدّم مع الجهة لا مع المدقق:
    /// «لم يبدأ» بلا ملفات، ثم «قيد العمل» من أول رفع، ثم «بانتظار المدقق» إذا اكتملت الأدلة
    /// بملفات أجازها الوكيل، فلا «معتمد نهائي» ولا «تم رفضه» قبل قرار رئيس الوحدة.
    /// </summary>
    public enum ItemBoardState
    {
        /// <summary>لم يُرفع له ملف أصلاً: فلا شيء وصل من الجهة بعد.</summary>
        NoEvidence,

        /// <summary>رُفع له ملف وما زال في الطريق: أدلة غير معلَنة، أو ملف لم يُجزه الوكيل.</summary>
        InWork,

        /// <summary>أدلته مكتملة بكل ملفاته أجازها الوكيل، فلم يبقَ إلا تدقيق الدكتور.</summary>
        AwaitingFinal,

        /// <summary>رفضه الدكتور بعد أن أجازَه الوكيل: فعاد إلى الجهة لإجراء تصحيحي.</summary>
        Rejected,

        /// <summary>أدلته مكتملة، وأجازَها الوكيل، ووقّعها الدكتور جميعاً.</summary>
        Closed
    }

    /// <summary>
    /// ما تحتاجه لوحة الجهة عن بند واحد: حالته، وأدلته التي أعلنها ممثلها، وأدلته التي يغطيها
    /// ملف أجازَه الوكيل. الشطب في اللوحة على الثانية لا على الأولى، فالإعلان وحده لا يُمرّ البند.
    /// </summary>
    public sealed record ItemBoardFacts(
        ItemBoardState State,
        IReadOnlyList<string> CoveredEvidence,
        IReadOnlyList<string> VerifiedEvidence);

    /// <summary>
    /// حالة البند على شاشة رئيس وحدة التطوير وضمان الجودة: أربع حالات لا خمس.
    /// فلا «قيد العمل» هنا، لأن الوكيل لا يرفع إلى رئيس الوحدة إلا ملفاً بلغت دقته 90% فأعلى؛
    /// فأي ملف يصل إليها هو إمّا ينتظر توقيعها، أو وقّعته، أو رفضته.
    /// </summary>
    public enum ReviewState
    {
        /// <summary>لم يصل منه إلى هذه الشاشة شيء: فلا تُفتح خانته أصلاً.</summary>
        NotUploaded,

        /// <summary>ملفات أجازها الوكيل، فلم يبقَ على البند إلا توقيع رئيس الوحدة.</summary>
        AwaitingReview,

        /// <summary>رفضه رئيس الوحدة بعد إجازة الوكيل، فعاد إلى الجهة لإجراء تصحيحي.</summary>
        Rejected,

        /// <summary>وقّع رئيس الوحدة كل ملفاته المجازة.</summary>
        Approved
    }

    /// <summary>ملف دليل داخل بند الجهة في شاشة التدقيق النهائي، بقرار الوكيل وقراري معاً.</summary>
    public sealed record ReviewFileRow(
        int SubmissionId,
        int AuditItemId,
        string FileName,
        string Extension,
        long FileSize,
        /// <summary>بلا درجة حين تُجيز الملفاتَ يدٌ تجاوزت بوابة الوكيل، فلا تُنتحل درجة لم تُحسب.</summary>
        int? AgentScore,
        AgentOutcome AgentOutcome,
        string? AgentFindings,
        string? ProvidedEvidence,
        string UploaderName,
        string UploaderEmployeeId,
        DateTime UploadedAt,
        FinalOutcome FinalOutcome,
        string? FinalNote,
        string? ReviewerName,
        string? ReviewerEmployeeId,
        DateTime? ReviewedAt);

    /// <summary>
    /// بند من بنود الجهة كما تفتحه شاشة التدقيق النهائي: اسمه ورمزه وعدد ما وصل منه وحالته.
    /// الملفات هنا ما أجازَه الوكيل فقط عند 90% فأعلى، فلا يُعرض على رئيس الوحدة سواه.
    /// </summary>
    public sealed record ReviewItemRow(
        int AuditItemId,
        int ItemNumber,
        string Title,
        string IsoReference,
        ReviewState State,
        IReadOnlyList<ReviewFileRow> Files,
        int HeldFileCount)
    {
        /// <summary>هل الخانة قابلة للفتح؟ ما لم يصل منه شيء لا يُفتح.</summary>
        public bool IsOpenable => State != ReviewState.NotUploaded;

        /// <summary>هل يقبل هذا البند توقيعاً الآن؟ الرفض والإغلاق ليسا موضع قرار بعد.</summary>
        public bool IsDecidable => State == ReviewState.AwaitingReview;

        /// <summary>سبب الرفض كما سجّله رئيس الوحدة على أحد ملفات البند المجازة.</summary>
        public string? RejectionReason =>
            State != ReviewState.Rejected
                ? null
                : Files.FirstOrDefault(f => f.FinalOutcome == FinalOutcome.Rejected)?.FinalNote;
    }

    /// <summary>ملف دليل على القرص كما تحتاجه نقطة الدخول لعرضه، مع جهته لحقّ الاطلاع.</summary>
    public sealed record EvidenceFileRef(string FullPath, string FileName, string Extension, string DepartmentId);

    /// <summary>
    /// حالة جهة واحدة كما تظهر في جدول إحصائيات التدقيق النهائي. الأرقام كلها بنود لا ملفات،
    /// و«تم رفعها» هو ما وصل منها إلى الشاشة: أي البنود التي أجاز الوكيل ملفاً واحداً على الأقل منها.
    /// ثم يتوزّع بعدَه بين ما ينتظر التوقيع وما رُفض وما أُغلق.
    /// </summary>
    public sealed record DepartmentAuditStat(
        string DepartmentId,
        string DepartmentName,
        int TotalItems,
        int ItemsUploaded,
        int ItemsAwaitingReview,
        int ItemsRejected,
        int ItemsApproved)
    {
        /// <summary>نسبة الإنجاز عند المسؤول: البنود التي وقّعها هو نهائياً من إجمالي بنود الجهة.</summary>
        public int CompletionPercent => TotalItems == 0 ? 0 : (int)Math.Round(ItemsApproved * 100.0 / TotalItems);
    }

    /// <summary>
    /// بندٌ أغلقه التدقيق البشري: كل ملف أجازَه الوكيل فيه وقّعَه المدقّق «مقبولاً»، فلا انتظار
    /// ولا رفضَ في شيء من ملفاته. ومع الجهة ورمز البند داخلها كما تعرضهما شاشة التدقيق النهائي.
    /// </summary>
    public sealed record ApprovedItemRow(
        string DepartmentId,
        string DepartmentName,
        int AuditItemId,
        int ItemNumber,
        string Title,
        string IsoReference,
        IReadOnlyList<ReviewFileRow> Files)
    {
        /// <summary>أحدث توقيع في ملفاته: به يُؤرَّخ اعتماد البند في السجل.</summary>
        public DateTime? ApprovedAt => Files.Max(f => f.ReviewedAt);

        /// <summary>من وقّع البند: ملفاتُه كلها توقيعُ مدقّقٍ واحد حين صار معتمداً.</summary>
        public string? ReviewerName => Files.FirstOrDefault(f => f.FinalOutcome == FinalOutcome.Approved)?.ReviewerName;

        public string? ReviewerEmployeeId => Files.FirstOrDefault(f => f.FinalOutcome == FinalOutcome.Approved)?.ReviewerEmployeeId;
    }

    public interface IEvidenceStore
    {
        Task<EvidenceSubmission> SaveAsync(NewEvidenceUpload upload, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvidenceSubmission>> ListForDepartmentAsync(
            string departmentId,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(int submissionId, CancellationToken cancellationToken = default);
        Task DeleteForItemAsync(int auditItemId, CancellationToken cancellationToken = default);

        /// <summary>يبطل قرار الوكيل عن بنود لم يعد ملفها مكتملاً، فينتظر فحصاً جديداً.</summary>
        Task InvalidateItemOutcomeAsync(int auditItemId, CancellationToken cancellationToken = default);

        /// <summary>
        /// الأدلة التي أعلن ممثل الجهة أنها مرفوعة للبند. تُكتب على كل صفوف البند قرارُ وكيله
        /// على البند كله لا على ملف منه، فتلغى كلها معاً عند أي تغيير في الملفات.
        /// </summary>
        Task SetItemEvidenceAsync(
            int auditItemId,
            IReadOnlyCollection<string> providedEvidence,
            CancellationToken cancellationToken = default);

        /// <summary>يكتب درجة البند وقرار البوابة على كل ملفات البند دفعة واحدة.</summary>
        Task ApplyAgentOutcomeAsync(
            int auditItemId,
            int score,
            string? findings,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// تجاوز بوابة الوكيل بضغطة يد واحدة: يُجيز كل ملفات البند بلا درجة ولا نتيجة آلية، ويترك في
        /// نص الملاحظات اسمَ من تجاوز ورقمه الوظيفي وتاريخ التجاوز، لأن أثر القرار يبقى في سجل التدقيق.
        /// </summary>
        Task<string> SkipAgentAuditAsync(
            int auditItemId,
            string actorName,
            string actorEmployeeId,
            CancellationToken cancellationToken = default);

        Task<byte[]?> ReadBytesAsync(int submissionId, CancellationToken cancellationToken = default);

        /// <summary>مرجع ملف على القرص ليرسله نقاط الدخول مباشرة دون تحميله في الذاكرة.</summary>
        Task<EvidenceFileRef?> GetFileRefAsync(int submissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// مرجع ملفٍ وقّعه المدقّق البشري «مقبولاً» وحده: شاشة النتيجة النهائية لا تفتح ملفاً
        /// لم يُعتمد بعد، فلا رقم ملف عندها يكشف بنداً ما زال عند صاحبه.
        /// </summary>
        Task<EvidenceFileRef?> GetApprovedFileRefAsync(int submissionId, CancellationToken cancellationToken = default);

        /// <summary>حالة بنود الجهة وأدلَتها المعلَنة والمجازة على لوحها: هي ومسار التدقيق النهائي حساباً واحداً.</summary>
        Task<IReadOnlyDictionary<int, ItemBoardFacts>> GetItemBoardFactsAsync(
            string departmentId,
            CancellationToken cancellationToken = default);

        /// <summary>بنود جهة واحدة بأربع حالات وبحالها، لما تُفتح الأرقام من جدول الإحصائيات في شاشة التدقيق النهائي.</summary>
        Task<IReadOnlyList<ReviewItemRow>> GetReviewBoardAsync(
            string departmentId,
            CancellationToken cancellationToken = default);

        Task ApplyFinalOutcomeAsync(
            int submissionId,
            FinalOutcome outcome,
            string? note,
            string reviewerEmployeeId,
            string reviewerName,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DepartmentAuditStat>> GetDepartmentStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>بنود كل الجهات التي أغلقها التوقيع البشري: لا ملف فيها بلا «مقبول»، ولا بند ناقص.</summary>
        Task<IReadOnlyList<ApprovedItemRow>> GetFinallyApprovedAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// مخزن أدلة على القرص: كل جهة ومجلدها في شجرة تحت App_Data/Evidence، والجدول يحمل المسار النسبي فقط.
    /// الاسم المولَّد عشوائي حتى لا يكتب ملفان متماثلَي الاسم فوق بعضهما ولا يُقرأ مسار خارج المخزن.
    /// </summary>
    public sealed class EvidenceStorage
    {
        public EvidenceStorage(IWebHostEnvironment env)
        {
            Root = Path.Combine(env.ContentRootPath, "App_Data", "Evidence");
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string Store(string departmentId, int auditItemId, string extension, byte[] bytes)
        {
            var folder = Path.Combine(Root, Sanitize(departmentId), auditItemId.ToString());
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{Sanitize(extension)}";
            File.WriteAllBytes(Path.Combine(folder, fileName), bytes);
            return Path.GetRelativePath(Root, Path.Combine(folder, fileName)).Replace('\\', '/');
        }

        /// <summary>المسار النسبي يأتي من الجدول، فلا يُقبل منه الخروج من جذر المخزن لو كُتب يدوياً.</summary>
        public string FullPath(string relativePath)
        {
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Root));
            var full = Path.GetFullPath(Path.Combine(root, relativePath));

            return full.Equals(root, StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                ? full
                : string.Empty;
        }

        public byte[]? Read(string relativePath)
        {
            var full = FullPath(relativePath);
            return full.Length > 0 && File.Exists(full) ? File.ReadAllBytes(full) : null;
        }

        public void Delete(string relativePath)
        {
            var full = FullPath(relativePath);
            if (full.Length > 0 && File.Exists(full))
                File.Delete(full);
        }

        private static string Sanitize(string value) =>
            string.Concat(value.Where(c => !Path.GetInvalidFileNameChars().Contains(c) && c != '/' && c != '\\'));
    }

    public sealed class EfEvidenceStore(
        IDbContextFactory<AppDbContext> contextFactory,
        EvidenceStorage storage) : IEvidenceStore
    {
        public async Task<EvidenceSubmission> SaveAsync(
            NewEvidenceUpload upload,
            CancellationToken cancellationToken = default)
        {
            var relativePath = storage.Store(upload.DepartmentId, upload.AuditItemId, upload.Extension, upload.Bytes);

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var submission = new EvidenceSubmission
            {
                DepartmentId = upload.DepartmentId,
                AuditItemId = upload.AuditItemId,
                FileName = upload.FileName,
                Extension = upload.Extension,
                FileSize = upload.Bytes.LongLength,
                StoredPath = relativePath,
                UploaderEmployeeId = upload.UploaderEmployeeId,
                UploaderName = upload.UploaderName,
                UploadedAt = DateTime.Now,
                AgentOutcome = AgentOutcome.NotChecked,
                FinalOutcome = FinalOutcome.NotReviewed
            };

            db.EvidenceSubmissions.Add(submission);

            // الرفع حدث يخص الجهة كلها، فيسجَّل في سجلها ويظهر في صفحة إشعارات ممثلها ومدققها.
            await NotifyItemAsync(
                db, upload.AuditItemId, NotificationKind.Uploaded, upload.FileName, null,
                upload.UploaderName, null, submission.UploadedAt, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            return submission;
        }

        public async Task<IReadOnlyList<EvidenceSubmission>> ListForDepartmentAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await db.EvidenceSubmissions.AsNoTracking()
                .Where(s => s.DepartmentId == departmentId)
                .OrderBy(s => s.AuditItemId).ThenBy(s => s.UploadedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteAsync(int submissionId, CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var submission = await db.EvidenceSubmissions
                .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);
            if (submission is null)
                return;

            db.EvidenceSubmissions.Remove(submission);
            await db.SaveChangesAsync(cancellationToken);
            storage.Delete(submission.StoredPath);
        }

        public async Task DeleteForItemAsync(int auditItemId, CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var submissions = await db.EvidenceSubmissions
                .Where(s => s.AuditItemId == auditItemId)
                .ToListAsync(cancellationToken);
            if (submissions.Count == 0)
                return;

            db.EvidenceSubmissions.RemoveRange(submissions);
            await db.SaveChangesAsync(cancellationToken);
            foreach (var submission in submissions)
                storage.Delete(submission.StoredPath);
        }

        public async Task InvalidateItemOutcomeAsync(int auditItemId, CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            await db.EvidenceSubmissions
                .Where(s => s.AuditItemId == auditItemId && s.AgentOutcome != AgentOutcome.NotChecked)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.AgentOutcome, AgentOutcome.NotChecked)
                    .SetProperty(s => s.AgentScore, (int?)null)
                    .SetProperty(s => s.AgentDefectRate, (int?)null)
                    .SetProperty(s => s.AgentFindings, (string?)null)
                    .SetProperty(s => s.AgentCheckedAt, (DateTime?)null)
                    .SetProperty(s => s.FinalOutcome, FinalOutcome.NotReviewed)
                    .SetProperty(s => s.FinalNote, (string?)null)
                    .SetProperty(s => s.ReviewerEmployeeId, (string?)null)
                    .SetProperty(s => s.ReviewerName, (string?)null)
                    .SetProperty(s => s.ReviewedAt, (DateTime?)null), cancellationToken);
        }

        public async Task SetItemEvidenceAsync(
            int auditItemId,
            IReadOnlyCollection<string> providedEvidence,
            CancellationToken cancellationToken = default)
        {
            var text = providedEvidence.Count == 0 ? null : string.Join('\n', providedEvidence);

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            await db.EvidenceSubmissions
                .Where(s => s.AuditItemId == auditItemId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.ProvidedEvidence, text), cancellationToken);
        }

        public async Task ApplyAgentOutcomeAsync(
            int auditItemId,
            int score,
            string? findings,
            CancellationToken cancellationToken = default)
        {
            var outcome = AgentGate.IsApproved(score) ? AgentOutcome.Approved : AgentOutcome.Rejected;
            var checkedAt = DateTime.Now;

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            await db.EvidenceSubmissions
                .Where(s => s.AuditItemId == auditItemId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.AgentScore, score)
                    .SetProperty(s => s.AgentDefectRate, 100 - score)
                    .SetProperty(s => s.AgentOutcome, outcome)
                    .SetProperty(s => s.AgentFindings, findings)
                    .SetProperty(s => s.AgentCheckedAt, checkedAt), cancellationToken);

            await NotifyItemAsync(
                db, auditItemId,
                outcome == AgentOutcome.Approved ? NotificationKind.AgentPassed : NotificationKind.AgentFailed,
                null, score, null, null, checkedAt, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<string> SkipAgentAuditAsync(
            int auditItemId,
            string actorName,
            string actorEmployeeId,
            CancellationToken cancellationToken = default)
        {
            // لا درجة تُكتب: انتحال درجة وكيل في سجل التدقيق كذب على رئيس الوحدة، والملاحظة تقول الحقيقة.
            var note = $"تجاوز يدوي لبوابة التدقيق الآلي: {actorName} (الرقم الوظيفي {actorEmployeeId}) " +
                       $"أجاز البند بلا فحص آلي بتاريخ {DateTime.Now:yyyy/MM/dd HH:mm}.";
            var checkedAt = DateTime.Now;

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            await db.EvidenceSubmissions
                .Where(s => s.AuditItemId == auditItemId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.AgentScore, (int?)null)
                    .SetProperty(s => s.AgentDefectRate, (int?)null)
                    .SetProperty(s => s.AgentOutcome, AgentOutcome.Approved)
                    .SetProperty(s => s.AgentFindings, note)
                    .SetProperty(s => s.AgentCheckedAt, checkedAt), cancellationToken);

            await NotifyItemAsync(
                db, auditItemId, NotificationKind.Overridden, null, null,
                actorName, note, checkedAt, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            return note;
        }

        public async Task<byte[]?> ReadBytesAsync(int submissionId, CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var path = await db.EvidenceSubmissions.AsNoTracking()
                .Where(s => s.Id == submissionId)
                .Select(s => s.StoredPath)
                .FirstOrDefaultAsync(cancellationToken);

            return path is null ? null : storage.Read(path);
        }

        public Task<EvidenceFileRef?> GetFileRefAsync(
            int submissionId,
            CancellationToken cancellationToken = default) => LoadFileRefAsync(submissionId, false, cancellationToken);

        public Task<EvidenceFileRef?> GetApprovedFileRefAsync(
            int submissionId,
            CancellationToken cancellationToken = default) => LoadFileRefAsync(submissionId, true, cancellationToken);

        private async Task<EvidenceFileRef?> LoadFileRefAsync(
            int submissionId,
            bool approvedOnly,
            CancellationToken cancellationToken)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var query = db.EvidenceSubmissions.AsNoTracking().Where(s => s.Id == submissionId);
            if (approvedOnly)
                query = query.Where(s => s.FinalOutcome == FinalOutcome.Approved);

            var row = await query
                .Select(s => new { s.StoredPath, s.FileName, s.Extension, s.DepartmentId })
                .FirstOrDefaultAsync(cancellationToken);
            if (row is null)
                return null;

            var full = storage.FullPath(row.StoredPath);
            return full.Length > 0 && File.Exists(full)
                ? new EvidenceFileRef(full, row.FileName, row.Extension, row.DepartmentId)
                : null;
        }

        public async Task<IReadOnlyList<ReviewItemRow>> GetReviewBoardAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            // الكتالوج أولاً ثم الملفات: البند الذي لم يجُز الوكيل ملفاته لا يصل إلى هذه الشاشة أصلاً.
            var catalog = await db.AuditItems.AsNoTracking()
                .Where(i => i.DepartmentId == departmentId)
                .OrderBy(i => i.SortOrder)
                .Select(i => new { i.Id, i.Title, i.IsoReference })
                .ToListAsync(cancellationToken);

            var rows = await db.EvidenceSubmissions.AsNoTracking()
                .Where(s => s.DepartmentId == departmentId)
                .OrderBy(s => s.AuditItemId).ThenBy(s => s.UploadedAt)
                .Select(s => new ReviewFileRow(
                    s.Id,
                    s.AuditItemId,
                    s.FileName,
                    s.Extension,
                    s.FileSize,
                    s.AgentScore,
                    s.AgentOutcome,
                    s.AgentFindings,
                    s.ProvidedEvidence,
                    s.UploaderName,
                    s.UploaderEmployeeId,
                    s.UploadedAt,
                    s.FinalOutcome,
                    s.FinalNote,
                    s.ReviewerName,
                    s.ReviewerEmployeeId,
                    s.ReviewedAt))
                .ToListAsync(cancellationToken);

            var byItem = rows.GroupBy(f => f.AuditItemId).ToDictionary(g => g.Key, g => g.ToList());

            // الرمز هو ترتيب البند داخل جهته كما تعرضه لوحة الجهة، لا مترقيم الزرع العام.
            return catalog.Select((i, index) =>
            {
                var allFiles = byItem.TryGetValue(i.Id, out var found) ? found : [];
                var passed = allFiles.Where(f => f.AgentOutcome == AgentOutcome.Approved).ToList();

                return new ReviewItemRow(
                    i.Id,
                    index + 1,
                    i.Title,
                    i.IsoReference,
                    ClassifyForReview(passed.Select(f => f.FinalOutcome).ToList()),
                    passed,
                    allFiles.Count - passed.Count);
            }).ToList();
        }

        public async Task<IReadOnlyDictionary<int, ItemBoardFacts>> GetItemBoardFactsAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            var files = await db.EvidenceSubmissions.AsNoTracking()
                .Where(s => s.DepartmentId == departmentId)
                .Select(s => new FileState(s.AuditItemId, s.AgentOutcome, s.FinalOutcome, s.ProvidedEvidence))
                .ToListAsync(cancellationToken);

            var required = await RequiredEvidenceByItemAsync(db, departmentId, cancellationToken);

            return files
                .GroupBy(f => f.AuditItemId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var item = new ItemFacts(
                            g.Select(f => (f.Agent, f.Final, f.Provided)).ToList(),
                            required.TryGetValue(g.Key, out var need) ? need : []);

                        return new ItemBoardFacts(
                            Classify(item),
                            CoveredLines(item, approvedOnly: false),
                            CoveredLines(item, approvedOnly: true));
                    });
        }

        private static async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> RequiredEvidenceByItemAsync(
            AppDbContext db,
            string departmentId,
            CancellationToken cancellationToken)
        {
            return (await db.EvidenceRequirements.AsNoTracking()
                    .Where(r => r.AuditItem.DepartmentId == departmentId)
                    .OrderBy(r => r.AuditItemId).ThenBy(r => r.SortOrder)
                    .Select(r => new { r.AuditItemId, r.Text })
                    .ToListAsync(cancellationToken))
                .GroupBy(r => r.AuditItemId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(r => r.Text).ToList());
        }

        public async Task ApplyFinalOutcomeAsync(
            int submissionId,
            FinalOutcome outcome,
            string? note,
            string reviewerEmployeeId,
            string reviewerName,
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            // ملف لا يقبله الوكيل لا يُوقَّع، وملف غير موجود لا حدث له: فلا سطر إشعار بلا تغيير.
            var file = await db.EvidenceSubmissions.AsNoTracking()
                .Where(s => s.Id == submissionId && s.AgentOutcome == AgentOutcome.Approved)
                .Select(s => new { s.FileName, s.AuditItemId })
                .FirstOrDefaultAsync(cancellationToken);
            if (file is null)
                return;

            var reviewedAt = DateTime.Now;
            await db.EvidenceSubmissions
                .Where(s => s.Id == submissionId && s.AgentOutcome == AgentOutcome.Approved)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.FinalOutcome, outcome)
                    .SetProperty(s => s.FinalNote, note)
                    .SetProperty(s => s.ReviewerEmployeeId, reviewerEmployeeId)
                    .SetProperty(s => s.ReviewerName, reviewerName)
                    .SetProperty(s => s.ReviewedAt, reviewedAt), cancellationToken);

            await NotifyItemAsync(
                db, file.AuditItemId,
                outcome == FinalOutcome.Approved ? NotificationKind.Approved : NotificationKind.Rejected,
                file.FileName, null, reviewerName, note, reviewedAt, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DepartmentAuditStat>> GetDepartmentStatsAsync(
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            var departments = await db.Departments.AsNoTracking()
                .OrderBy(d => d.DepartmentName)
                .Select(d => new { d.DepartmentId, d.DepartmentName, TotalItems = d.AuditItems.Count })
                .ToListAsync(cancellationToken);

            // التجميع في الذاكرة: عدد الملفات في نموذج أولي صغير، وحالة البند تُشتق من قرارات كل ملفاته
            // فلو تُرجمت إلى SQL لأصبحت قراءة الجدول أوضح منها.
            var rows = await db.EvidenceSubmissions.AsNoTracking()
                .Select(s => new { s.DepartmentId, s.AuditItemId, s.AgentOutcome, s.FinalOutcome })
                .ToListAsync(cancellationToken);

            var itemStates = rows
                .GroupBy(r => r.DepartmentId)
                .ToDictionary(
                    byDepartment => byDepartment.Key,
                    byDepartment => byDepartment
                        .GroupBy(r => r.AuditItemId)
                        .Select(item => ClassifyForReview(item
                            .Where(f => f.AgentOutcome == AgentOutcome.Approved)
                            .Select(f => f.FinalOutcome)
                            .ToList()))
                        .ToList());

            return departments.Select(d =>
            {
                // البنود المجرّبة كلها بقرار الوكيل: فما لم يُجز شيئاً من ملفاته لا يدخل العدّ في أي خانة.
                var items = itemStates.TryGetValue(d.DepartmentId, out var found) ? found : [];
                return new DepartmentAuditStat(
                    d.DepartmentId,
                    d.DepartmentName,
                    d.TotalItems,
                    items.Count(i => i != ReviewState.NotUploaded),
                    items.Count(i => i == ReviewState.AwaitingReview),
                    items.Count(i => i == ReviewState.Rejected),
                    items.Count(i => i == ReviewState.Approved));
            }).ToList();
        }

        public async Task<IReadOnlyList<ApprovedItemRow>> GetFinallyApprovedAsync(
            CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            // الجهات فالبنود بترتيبها داخل كل جهة: الرمز المعروض مترقيمُ الجهة لا مترقيمُ الزرع العام.
            var departments = await db.Departments.AsNoTracking()
                .OrderBy(d => d.DepartmentName)
                .Select(d => new { d.DepartmentId, d.DepartmentName })
                .ToListAsync(cancellationToken);

            var itemsByDepartment = (await db.AuditItems.AsNoTracking()
                .OrderBy(i => i.DepartmentId).ThenBy(i => i.SortOrder)
                .Select(i => new { i.Id, i.DepartmentId, i.Title, i.IsoReference })
                .ToListAsync(cancellationToken))
                .GroupBy(i => i.DepartmentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ما أجازَه الوكيل وحده يدخل العدّ: فالبند المعتمد هو ما وقّعَ المدقّق كل ملفٍ وصلَه منه.
            var filesByItem = (await db.EvidenceSubmissions.AsNoTracking()
                .Where(s => s.AgentOutcome == AgentOutcome.Approved)
                .OrderBy(s => s.AuditItemId).ThenBy(s => s.UploadedAt)
                .Select(s => new ReviewFileRow(
                    s.Id,
                    s.AuditItemId,
                    s.FileName,
                    s.Extension,
                    s.FileSize,
                    s.AgentScore,
                    s.AgentOutcome,
                    s.AgentFindings,
                    s.ProvidedEvidence,
                    s.UploaderName,
                    s.UploaderEmployeeId,
                    s.UploadedAt,
                    s.FinalOutcome,
                    s.FinalNote,
                    s.ReviewerName,
                    s.ReviewerEmployeeId,
                    s.ReviewedAt))
                .ToListAsync(cancellationToken))
                .GroupBy(f => f.AuditItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var approved = new List<ApprovedItemRow>();
            foreach (var department in departments)
            {
                if (!itemsByDepartment.TryGetValue(department.DepartmentId, out var items))
                    continue;

                foreach (var (item, index) in items.Select((definition, position) => (definition, position)))
                {
                    // لا بند بلا ملف مجاز، ولا بند بقيت منه ملفاً تنتظر توقيعا أو رُفضت: الإغلاق يعني «مقبول» كلها.
                    if (!filesByItem.TryGetValue(item.Id, out var files)
                        || !files.All(f => f.FinalOutcome == FinalOutcome.Approved))
                        continue;

                    approved.Add(new ApprovedItemRow(
                        department.DepartmentId,
                        department.DepartmentName,
                        item.Id,
                        index + 1,
                        item.Title,
                        item.IsoReference,
                        files));
                }
            }

            return approved;
        }

        /// <summary>
        /// يضيف سطر إشعار عن بند، بعد أن يقرأ عنوانه واسم جهته من الكتالوج: العنوان والجهة يُحفظان
        /// في السطر كما هما، فلا يتبع سطرُ السجل تغييراً لاحقاً في الكتالوج. والسطر يُحفظ مع التغيير
        /// نفسه في نداء واحد، فلا يبقى حدث بلا أثر أو أثر بلا حدث.
        /// </summary>
        private static async Task NotifyItemAsync(
            AppDbContext db,
            int auditItemId,
            NotificationKind kind,
            string? fileName,
            int? score,
            string? actorName,
            string? note,
            DateTime createdAt,
            CancellationToken cancellationToken)
        {
            var facts = await db.AuditItems.AsNoTracking()
                .Where(i => i.Id == auditItemId)
                .Select(i => new { i.Title, i.DepartmentId, DepartmentName = i.Department.DepartmentName })
                .FirstOrDefaultAsync(cancellationToken);
            if (facts is null)
                return;

            db.Notifications.Add(new Notification
            {
                Kind = kind,
                DepartmentId = facts.DepartmentId,
                DepartmentName = facts.DepartmentName,
                ItemTitle = facts.Title,
                FileName = fileName,
                ActorName = actorName,
                AgentScore = score,
                Note = note,
                CreatedAt = createdAt
            });
        }

        private sealed record FileState(
            int AuditItemId,
            AgentOutcome Agent,
            FinalOutcome Final,
            string? Provided);

        /// <summary>
        /// حالة البند على شاشة رئيس الوحدة من قراراتها هي لا من قرار الوكيل: فالملف لم يصل إليها
        /// إلا مجازاً عند 90% فأعلى، وأي ملف عندها إمّا لم توقّع عليه بعد، أو وقّعتِه، أو رفضتِه.
        /// </summary>
        private static ReviewState ClassifyForReview(IReadOnlyList<FinalOutcome> decisions)
        {
            if (decisions.Count == 0)
                return ReviewState.NotUploaded;

            // رفض واحد يكفي: البند عاد إلى الجهة لإجراء تصحيحي، فلا ينفع أن تُوقَّع بقيّته.
            if (decisions.Any(d => d == FinalOutcome.Rejected))
                return ReviewState.Rejected;

            return decisions.All(d => d == FinalOutcome.Approved)
                ? ReviewState.Approved
                : ReviewState.AwaitingReview;
        }

        /// <summary>
        /// حالة البند في واحدة فقط: أي ملف مرفوع ينقله إلى «قيد العمل»، ولا يخرج منها إلا حين
        /// يُعلن ممثل الجهة كل أدلته ويُجاز الوكيل كل ملفاته؛ ثم يبقى عند الدكتور لا يُغلق ولا يُرفض سواه.
        /// </summary>
        private static ItemBoardState Classify(ItemFacts item)
        {
            if (item.Files.Count == 0)
                return ItemBoardState.NoEvidence;

            // رفض الدكتور يعلو على أي حال أخرى: فالبند عاد للجهة حتى لو بقيت ملفات أخرى بلا توقيع.
            if (item.Files.Any(f => f.Final == FinalOutcome.Rejected))
                return ItemBoardState.Rejected;

            // رُفع له شيء لكن الوكيل لم يُجز منه شيئاً بعد، أو رفضه كله: العمل ما زال عند الجهة.
            var approved = item.Files.Where(f => f.Agent == AgentOutcome.Approved).ToList();
            if (approved.Count == 0)
                return ItemBoardState.InWork;

            // إعلان الجهة لا يكفي: الدليل المحسوب هو ما يغطّيه ملف أجازَه الوكيل لا ما وُسم فقط.
            if (CoveredLines(item, approvedOnly: true).Count < item.RequiredEvidence.Count)
                return ItemBoardState.InWork;

            if (approved.Count < item.Files.Count)
                return ItemBoardState.InWork;

            return approved.All(f => f.Final == FinalOutcome.Approved)
                ? ItemBoardState.Closed
                : ItemBoardState.AwaitingFinal;
        }

        /// <summary>
        /// أدلة الكتالوج التي غطّاها الإعلان، بترتيب الكتالوج فلا تتغيّر موضعاً بتغيّر التوقيت.
        /// ومع approvedOnly تُحسب من صفوف الملفات التي أجازَها الوكيل وحدها، فهي ما يعتمده المسار.
        /// </summary>
        private static IReadOnlyList<string> CoveredLines(ItemFacts item, bool approvedOnly)
        {
            if (item.RequiredEvidence.Count == 0)
                return [];

            var sources = item.Files
                .Where(f => !approvedOnly || f.Agent == AgentOutcome.Approved)
                .Select(f => f.Provided);

            var declared = DeclaredLines(sources);
            return declared.Count == 0
                ? []
                : item.RequiredEvidence.Where(text => declared.Contains(text.Trim())).ToList();
        }

        private static HashSet<string> DeclaredLines(IEnumerable<string?> providedLines) =>
            providedLines
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .SelectMany(p => p!.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToHashSet();

        private sealed record ItemFacts(
            IReadOnlyList<(AgentOutcome Agent, FinalOutcome Final, string? Provided)> Files,
            IReadOnlyList<string> RequiredEvidence);
    }
}

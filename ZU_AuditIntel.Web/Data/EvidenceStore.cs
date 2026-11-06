using System.Globalization;
using Google.Cloud.Firestore;
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
        string SubmissionId,
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

        Task DeleteAsync(string submissionId, CancellationToken cancellationToken = default);
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

        Task<byte[]?> ReadBytesAsync(string submissionId, CancellationToken cancellationToken = default);

        /// <summary>مرجع ملف على القرص ليرسله نقاط الدخول مباشرة دون تحميله في الذاكرة.</summary>
        Task<EvidenceFileRef?> GetFileRefAsync(string submissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// مرجع ملفٍ وقّعه المدقّق البشري «مقبولاً» وحده: شاشة النتيجة النهائية لا تفتح ملفاً
        /// لم يُعتمد بعد، فلا رقم ملف عندها يكشف بنداً ما زال عند صاحبه.
        /// </summary>
        Task<EvidenceFileRef?> GetApprovedFileRefAsync(string submissionId, CancellationToken cancellationToken = default);

        /// <summary>حالة بنود الجهة وأدلَتها المعلَنة والمجازة على لوحها: هي ومسار التدقيق النهائي حساباً واحداً.</summary>
        Task<IReadOnlyDictionary<int, ItemBoardFacts>> GetItemBoardFactsAsync(
            string departmentId,
            CancellationToken cancellationToken = default);

        /// <summary>بنود جهة واحدة بأربع حالات وبحالها، لما تُفتح الأرقام من جدول الإحصائيات في شاشة التدقيق النهائي.</summary>
        Task<IReadOnlyList<ReviewItemRow>> GetReviewBoardAsync(
            string departmentId,
            CancellationToken cancellationToken = default);

        Task ApplyFinalOutcomeAsync(
            string submissionId,
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
    /// مخزن أدلة على القرص: كل جهة ومجلدها في شجرة تحت App_Data/Evidence، والوثيقة تحمل المسار
    /// النسبي فقط. الاسم المولَّد عشوائي حتى لا يكتب ملفان متماثلَي الاسم فوق بعضهما ولا يُقرأ مسار
    /// خارج المخزن.
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

        /// <summary>المسار النسبي يأتي من الوثيقة، فلا يُقبل منه الخروج من جذر المخزن لو كُتب يدوياً.</summary>
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

    /// <summary>
    /// مخزن الأدلة على Firestore: ملف الدليل الواحد وثيقةٌ في <c>submissions</c> تحمل قرار الوكيل
    /// وقرار التدقيق النهائي معاً. كل استعلام هنا شرطٌ واحد على حقلٍ علوي — جهة، أو بند، أو قرار —
    /// والترتيب والتجميع بعد ذلك في الذاكرة، فلا حاجة إلى فهرس مركّب لا سبيل إلى إنشائه.
    ///
    /// لا عدّادات حالة في وثيقة البند: الحالة تُشتق من صفوف ملفات البند وقت القراءة، فعدّادٌ يُنسى
    /// تحديثه رقمٌ خاطئ على شاشة تدقيق، والقرارات نفسها هي ما يوقّع عليه البشر لا ما يُشتق منها.
    /// </summary>
    public sealed class FirestoreEvidenceStore(
        FirestoreDb firestore,
        EvidenceStorage storage,
        INotificationStore notifications) : IEvidenceStore
    {
        /// <summary>أقصى ما تستوعبه دفعة كتابة واحدة، مع هامش لما يتّسع له النداء من تفرّعات.</summary>
        private const int BatchLimit = 400;

        private CollectionReference Submissions { get; } = firestore.Collection(AuditCollections.Submissions);
        private CollectionReference Items { get; } = firestore.Collection(AuditCollections.Items);
        private CollectionReference Departments { get; } = firestore.Collection(AuditCollections.Departments);

        public async Task<EvidenceSubmission> SaveAsync(
            NewEvidenceUpload upload,
            CancellationToken cancellationToken = default)
        {
            var relativePath = storage.Store(upload.DepartmentId, upload.AuditItemId, upload.Extension, upload.Bytes);

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

            var reference = Submissions.Document();
            submission.Id = reference.Id;
            await reference.SetAsync(submission, SetOptions.Overwrite, cancellationToken);

            // الرفع حدث يخص الجهة كلها، فيسجَّل في سجلها ويظهر في صفحة إشعارات ممثلها ومدققها.
            await NotifyAsync(
                upload.AuditItemId, NotificationKind.Uploaded, upload.FileName, null,
                upload.UploaderName, null, submission.UploadedAt, cancellationToken);

            return submission;
        }

        public async Task<IReadOnlyList<EvidenceSubmission>> ListForDepartmentAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            var files = await FilesForDepartmentAsync(departmentId, cancellationToken);

            return files
                .OrderBy(s => s.AuditItemId)
                .ThenBy(s => s.UploadedAt)
                .ThenBy(s => s.Id, StringComparer.Ordinal)
                .ToList();
        }

        public async Task DeleteAsync(string submissionId, CancellationToken cancellationToken = default)
        {
            var reference = Submissions.Document(submissionId);
            var submission = (await reference.GetSnapshotAsync(cancellationToken)).ConvertTo<EvidenceSubmission>();
            if (submission is null)
                return;

            await reference.DeleteAsync(precondition: null, cancellationToken: cancellationToken);
            storage.Delete(submission.StoredPath);
        }

        public async Task DeleteForItemAsync(int auditItemId, CancellationToken cancellationToken = default)
        {
            var documents = await DocumentsForItemAsync(auditItemId, cancellationToken);
            if (documents.Count == 0)
                return;

            var batch = firestore.StartBatch();
            foreach (var snapshot in documents)
                batch.Delete(snapshot.Reference);

            await batch.CommitAsync(cancellationToken);

            foreach (var snapshot in documents)
                storage.Delete(snapshot.GetValue<string>("storedPath"));
        }

        public async Task InvalidateItemOutcomeAsync(int auditItemId, CancellationToken cancellationToken = default)
        {
            var documents = (await DocumentsForItemAsync(auditItemId, cancellationToken))
                .Where(snapshot => snapshot.GetValue<string>("agentOutcome") != nameof(AgentOutcome.NotChecked))
                .ToList();

            await UpdateAsync(documents, new Dictionary<string, object?>
            {
                ["agentOutcome"] = nameof(AgentOutcome.NotChecked),
                ["agentScore"] = null,
                ["agentDefectRate"] = null,
                ["agentFindings"] = null,
                ["agentCheckedAt"] = null,
                ["finalOutcome"] = nameof(FinalOutcome.NotReviewed),
                ["finalNote"] = null,
                ["reviewerEmployeeId"] = null,
                ["reviewerName"] = null,
                ["reviewedAt"] = null
            }, cancellationToken);
        }

        public async Task SetItemEvidenceAsync(
            int auditItemId,
            IReadOnlyCollection<string> providedEvidence,
            CancellationToken cancellationToken = default)
        {
            var text = providedEvidence.Count == 0 ? null : string.Join('\n', providedEvidence);

            await UpdateAsync(
                await DocumentsForItemAsync(auditItemId, cancellationToken),
                new Dictionary<string, object?> { ["providedEvidence"] = text },
                cancellationToken);
        }

        public async Task ApplyAgentOutcomeAsync(
            int auditItemId,
            int score,
            string? findings,
            CancellationToken cancellationToken = default)
        {
            var outcome = AgentGate.IsApproved(score) ? AgentOutcome.Approved : AgentOutcome.Rejected;
            var checkedAt = DateTime.Now;

            await UpdateAsync(
                await DocumentsForItemAsync(auditItemId, cancellationToken),
                new Dictionary<string, object?>
                {
                    ["agentScore"] = score,
                    ["agentDefectRate"] = 100 - score,
                    ["agentOutcome"] = outcome.ToString(),
                    ["agentFindings"] = findings,
                    ["agentCheckedAt"] = Moment(checkedAt)
                },
                cancellationToken);

            await NotifyAsync(
                auditItemId,
                outcome == AgentOutcome.Approved ? NotificationKind.AgentPassed : NotificationKind.AgentFailed,
                null, score, null, null, checkedAt, cancellationToken);
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

            await UpdateAsync(
                await DocumentsForItemAsync(auditItemId, cancellationToken),
                new Dictionary<string, object?>
                {
                    ["agentScore"] = null,
                    ["agentDefectRate"] = null,
                    ["agentOutcome"] = AgentOutcome.Approved.ToString(),
                    ["agentFindings"] = note,
                    ["agentCheckedAt"] = Moment(checkedAt)
                },
                cancellationToken);

            await NotifyAsync(
                auditItemId, NotificationKind.Overridden, null, null, actorName, note, checkedAt, cancellationToken);

            return note;
        }

        public async Task<byte[]?> ReadBytesAsync(string submissionId, CancellationToken cancellationToken = default)
        {
            var snapshot = await Submissions.Document(submissionId).GetSnapshotAsync(cancellationToken);

            // الوثيقة إن لم تكن فهي محذوفة: تقرأ null لا تستثني، فنقطة الملفات تردّ بأربعٍ لا وجدت.
            return snapshot.Exists ? storage.Read(snapshot.GetValue<string>("storedPath")) : null;
        }

        public Task<EvidenceFileRef?> GetFileRefAsync(
            string submissionId,
            CancellationToken cancellationToken = default) => LoadFileRefAsync(submissionId, false, cancellationToken);

        public Task<EvidenceFileRef?> GetApprovedFileRefAsync(
            string submissionId,
            CancellationToken cancellationToken = default) => LoadFileRefAsync(submissionId, true, cancellationToken);

        private async Task<EvidenceFileRef?> LoadFileRefAsync(
            string submissionId,
            bool approvedOnly,
            CancellationToken cancellationToken)
        {
            var submission = (await Submissions.Document(submissionId).GetSnapshotAsync(cancellationToken))
                .ConvertTo<EvidenceSubmission>();

            // ملف لم يُعتمد بعد لا يُفتح لصاحب النتيجة، وغير الموجود لا حدث له: فلا رقم ملف يكشف بنداً ناقصاً.
            if (submission is null || (approvedOnly && submission.FinalOutcome != FinalOutcome.Approved))
                return null;

            var full = storage.FullPath(submission.StoredPath);
            return full.Length > 0 && File.Exists(full)
                ? new EvidenceFileRef(full, submission.FileName, submission.Extension, submission.DepartmentId)
                : null;
        }

        public async Task<IReadOnlyList<ReviewItemRow>> GetReviewBoardAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            // الكتالوج أولاً ثم الملفات: البند الذي لم يجُز الوكيل ملفاته لا يصل إلى هذه الشاشة أصلاً.
            var catalog = await ItemsOfAsync(departmentId, cancellationToken);
            var rows = await FilesForDepartmentAsync(departmentId, cancellationToken);

            var byItem = rows
                .GroupBy(s => s.AuditItemId)
                .ToDictionary(g => g.Key, g => g.Select(ToReviewRow).ToList());

            // الرمز هو ترتيب البند داخل جهته كما تعرضه لوحة الجهة، لا مترقيم الزرع العام.
            return catalog
                .Select((item, index) =>
                {
                    var allFiles = byItem.TryGetValue(item.Id, out var found) ? found : [];
                    var passed = allFiles.Where(f => f.AgentOutcome == AgentOutcome.Approved).ToList();

                    return new ReviewItemRow(
                        item.Id,
                        index + 1,
                        item.Title,
                        item.IsoReference,
                        ClassifyForReview(passed.Select(f => f.FinalOutcome).ToList()),
                        passed,
                        allFiles.Count - passed.Count);
                })
                .ToList();
        }

        public async Task<IReadOnlyDictionary<int, ItemBoardFacts>> GetItemBoardFactsAsync(
            string departmentId,
            CancellationToken cancellationToken = default)
        {
            var files = await FilesForDepartmentAsync(departmentId, cancellationToken);
            var required = (await ItemsOfAsync(departmentId, cancellationToken))
                .ToDictionary(item => item.Id, item => item.RequiredEvidence);

            return files
                .GroupBy(s => s.AuditItemId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var item = new ItemFacts(
                            g.Select(f => (f.AgentOutcome, f.FinalOutcome, f.ProvidedEvidence)).ToList(),
                            required.TryGetValue(g.Key, out var need) ? need : []);

                        return new ItemBoardFacts(
                            Classify(item),
                            CoveredLines(item, approvedOnly: false),
                            CoveredLines(item, approvedOnly: true));
                    });
        }

        public async Task ApplyFinalOutcomeAsync(
            string submissionId,
            FinalOutcome outcome,
            string? note,
            string reviewerEmployeeId,
            string reviewerName,
            CancellationToken cancellationToken = default)
        {
            var reference = Submissions.Document(submissionId);

            // ملف لا يقبله الوكيل لا يُوقَّع، وملف غير موجود لا حدث له: فلا سطر إشعار بلا تغيير.
            var file = (await reference.GetSnapshotAsync(cancellationToken)).ConvertTo<EvidenceSubmission>();
            if (file is null || file.AgentOutcome != AgentOutcome.Approved)
                return;

            var reviewedAt = DateTime.Now;

            await reference.UpdateAsync(ToFields(new Dictionary<string, object?>
            {
                ["finalOutcome"] = outcome.ToString(),
                ["finalNote"] = note,
                ["reviewerEmployeeId"] = reviewerEmployeeId,
                ["reviewerName"] = reviewerName,
                ["reviewedAt"] = Moment(reviewedAt)
            }), cancellationToken: cancellationToken);

            await NotifyAsync(
                file.AuditItemId,
                outcome == FinalOutcome.Approved ? NotificationKind.Approved : NotificationKind.Rejected,
                file.FileName, null, reviewerName, note, reviewedAt, cancellationToken);
        }

        public async Task<IReadOnlyList<DepartmentAuditStat>> GetDepartmentStatsAsync(
            CancellationToken cancellationToken = default)
        {
            var departments = await DepartmentsOrderedAsync(cancellationToken);

            // التجميع في الذاكرة: عدد الملفات في نموذج أولي صغير، وحالة البند تُشتق من قرارات كل ملفاته
            // فلو فُصّلت في الاستعلام لأصبحت قراءة الوثائق أوضح منها.
            var files = (await Submissions.GetSnapshotAsync(cancellationToken)).Documents
                .Select(snapshot => snapshot.ConvertTo<EvidenceSubmission>()!);

            var itemStates = files
                .GroupBy(s => s.DepartmentId)
                .ToDictionary(
                    byDepartment => byDepartment.Key,
                    byDepartment => byDepartment
                        .GroupBy(s => s.AuditItemId)
                        .Select(item => ClassifyForReview(item
                            .Where(f => f.AgentOutcome == AgentOutcome.Approved)
                            .Select(f => f.FinalOutcome)
                            .ToList()))
                        .ToList());

            var stats = new List<DepartmentAuditStat>(departments.Count);
            foreach (var department in departments)
            {
                // البنود المجرّبة كلها بقرار الوكيل: فما لم يُجز شيئاً من ملفاته لا يدخل العدّ في أي خانة.
                var items = itemStates.TryGetValue(department.DepartmentId, out var found) ? found : [];
                stats.Add(new DepartmentAuditStat(
                    department.DepartmentId,
                    department.DepartmentName,
                    (await ItemsOfAsync(department.DepartmentId, cancellationToken)).Count,
                    items.Count(i => i != ReviewState.NotUploaded),
                    items.Count(i => i == ReviewState.AwaitingReview),
                    items.Count(i => i == ReviewState.Rejected),
                    items.Count(i => i == ReviewState.Approved)));
            }

            return stats;
        }

        public async Task<IReadOnlyList<ApprovedItemRow>> GetFinallyApprovedAsync(
            CancellationToken cancellationToken = default)
        {
            // الجهات فالبنود بترتيبها داخل كل جهة: الرمز المعروض مترقيمُ الجهة لا مترقيمُ الزرع العام.
            var departments = await DepartmentsOrderedAsync(cancellationToken);

            // ما أجازَه الوكيل وحده يدخل العدّ: فالبند المعتمد هو ما وقّعَ المدقّق كل ملفٍ وصلَه منه.
            var filesByItem = (await Submissions
                    .WhereEqualTo("agentOutcome", AgentOutcome.Approved.ToString())
                    .GetSnapshotAsync(cancellationToken)).Documents
                .Select(snapshot => snapshot.ConvertTo<EvidenceSubmission>()!)
                .GroupBy(s => s.AuditItemId)
                .ToDictionary(g => g.Key, g => g
                    .OrderBy(s => s.UploadedAt).ThenBy(s => s.Id, StringComparer.Ordinal)
                    .Select(ToReviewRow).ToList());

            var approved = new List<ApprovedItemRow>();
            foreach (var department in departments)
            {
                var items = await ItemsOfAsync(department.DepartmentId, cancellationToken);

                for (var index = 0; index < items.Count; index++)
                {
                    var item = items[index];

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
        /// سطر إشعار عن بند، بعد أن يُقرأ عنوانه واسم جهته من وثيقتيهما: العنوان والجهة يُحفظان في
        /// السطر كما هما لحظة الحدث، فلا يتبع سطرُ السجل تغييراً لاحقاً في الكتالوج.
        /// </summary>
        private async Task NotifyAsync(
            int auditItemId,
            NotificationKind kind,
            string? fileName,
            int? score,
            string? actorName,
            string? note,
            DateTime createdAt,
            CancellationToken cancellationToken)
        {
            var item = (await Items
                .Document(auditItemId.ToString(CultureInfo.InvariantCulture))
                .GetSnapshotAsync(cancellationToken)).ConvertTo<AuditItem>();
            if (item is null)
                return;

            var departmentName = (await Departments.Document(item.DepartmentId)
                .GetSnapshotAsync(cancellationToken)).GetValue<string>("departmentName");

            await notifications.RecordAsync(new Notification
            {
                Kind = kind,
                DepartmentId = item.DepartmentId,
                DepartmentName = departmentName,
                ItemTitle = item.Title,
                FileName = fileName,
                ActorName = actorName,
                AgentScore = score,
                Note = note,
                CreatedAt = createdAt
            }, cancellationToken);
        }

        private sealed record CatalogItem(int Id, string Title, string IsoReference, IReadOnlyList<string> RequiredEvidence);

        /// <summary>بنود جهة واحدة بترتيب اللوحة نفسه الذي تُعرض به الأرقام.</summary>
        private async Task<IReadOnlyList<CatalogItem>> ItemsOfAsync(
            string departmentId, CancellationToken cancellationToken)
        {
            var documents = await Items
                .WhereEqualTo("departmentId", departmentId)
                .GetSnapshotAsync(cancellationToken);

            return documents.Documents
                .Select(snapshot => snapshot.ConvertTo<AuditItem>()!)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Id)
                .Select(item => new CatalogItem(item.Id, item.Title, item.IsoReference, item.RequiredEvidence))
                .ToList();
        }

        /// <summary>ملفات جهة: شرطٌ واحد على الجهة، فالترتيب شأن محلي.</summary>
        private async Task<IReadOnlyList<EvidenceSubmission>> FilesForDepartmentAsync(
            string departmentId, CancellationToken cancellationToken)
        {
            var documents = await Submissions
                .WhereEqualTo("departmentId", departmentId)
                .GetSnapshotAsync(cancellationToken);

            return documents.Documents
                .Select(snapshot => snapshot.ConvertTo<EvidenceSubmission>()!)
                .ToList();
        }

        private async Task<IReadOnlyList<DocumentSnapshot>> DocumentsForItemAsync(
            int auditItemId, CancellationToken cancellationToken)
        {
            var documents = await Submissions
                .WhereEqualTo("auditItemId", (long)auditItemId)
                .GetSnapshotAsync(cancellationToken);

            return documents.Documents;
        }

        private async Task<IReadOnlyList<AuditDepartment>> DepartmentsOrderedAsync(
            CancellationToken cancellationToken)
        {
            var documents = await Departments.GetSnapshotAsync(cancellationToken);

            return documents.Documents
                .Select(snapshot => new AuditDepartment(
                    snapshot.Id,
                    snapshot.GetValue<string>("departmentName")))
                .OrderBy(department => department.DepartmentName, StringComparer.Ordinal)
                .ToList();
        }

        private static ReviewFileRow ToReviewRow(EvidenceSubmission s) => new(
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
            s.ReviewedAt);

        /// <summary>
        /// حقول مشتركة تُكتب على كل وثائق قائمة واحدة: لا «تحديث بشرط» في Firestore، فتُقرأ الوثائق
        /// أولاً ثم تُكتب في دفعات. أكثر ما هنا بضع عشرات من وثائق بند واحد، لا آلاف.
        /// </summary>
        private async Task UpdateAsync(
            IReadOnlyList<DocumentSnapshot> documents,
            IDictionary<string, object?> fields,
            CancellationToken cancellationToken)
        {
            var updates = ToFields(fields);
            var batch = firestore.StartBatch();
            var pending = 0;

            foreach (var snapshot in documents)
            {
                batch.Update(snapshot.Reference, updates);
                if (++pending == BatchLimit)
                {
                    await batch.CommitAsync(cancellationToken);
                    batch = firestore.StartBatch();
                    pending = 0;
                }
            }

            if (pending > 0)
                await batch.CommitAsync(cancellationToken);
        }

        /// <summary>
        /// الحقول التي تُكتب بقاموس مباشرة لا تمرّ بمحوّل النموذج، فتحتاج التوقيت لحظةً UTC كما
        /// يحوّلها هو: وإلا رفضتها الحزمة لأن وقت الجهاز محلي لا موحّد.
        /// </summary>
        private static Timestamp Moment(DateTime local) => Timestamp.FromDateTime(local.ToUniversalTime());

        /// <summary>
        /// الحقل الخالي يُكتب صراحةً null ولا يُحذف: كما كان العمود في القاعدة القديمة يساوي NULL،
        /// يقرأ منه النموذج لاحقاً «لا نتيجة بعد» لا «وثيقة ناقصة».
        /// </summary>
        private static IDictionary<string, object> ToFields(IDictionary<string, object?> fields) =>
            fields.ToDictionary(pair => pair.Key, pair => pair.Value!);

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

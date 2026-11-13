using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// مخزن الأدلة على القاعدة المحلية: صفٌّ لكل ملف يحمل قرار الوكيل وقرار التدقيق النهائي معاً،
    /// وبايتاته على قرص التطبيق نفسه. الحالة تُشتق من صفوف ملفات البند وقت القراءة لا من عدّادٍ
    /// يُنسى تحديثه، والكتابة على البند كله جملةٌ واحدة تمسّ صفوفه دفعةً لا قراءةً ثم دفعات.
    ///
    /// والكتالوج يُقرأ من نسخته المخبوءة: بنود الجهة وجهاتها ثوابت لا تتغيّر إلا بإعادة استيراد،
    /// فلا تُستهلك قراءةٌ من القاعدة لعرض رقم بندٍ أو عنوانه.
    /// </summary>
    public sealed class SqliteEvidenceStore(
        LocalDb db,
        LocalEvidenceStorage storage,
        INotificationStore notifications,
        IAuditCatalog catalog,
        ILogger<SqliteEvidenceStore> logger) : IEvidenceStore
    {
        public async Task<EvidenceSubmission> SaveAsync(
            NewEvidenceUpload upload, CancellationToken cancellationToken = default)
        {
            // المفتاح شجرةُ الجهة والبند نفسها التي كانت على القرص، فقيمة stored_path لا تتغيّر دلالتها.
            var key = await storage.PutAsync(
                upload.DepartmentId, upload.AuditItemId, upload.Extension, upload.Bytes, cancellationToken);

            var submission = new EvidenceSubmission
            {
                Id = Guid.NewGuid().ToString("N"),
                DepartmentId = upload.DepartmentId,
                AuditItemId = upload.AuditItemId,
                FileName = upload.FileName,
                Extension = upload.Extension,
                FileSize = upload.Bytes.LongLength,
                StoredPath = key,
                UploaderEmployeeId = upload.UploaderEmployeeId,
                UploaderName = upload.UploaderName,
                UploadedAt = DateTime.Now,
                AgentOutcome = AgentOutcome.NotChecked,
                FinalOutcome = FinalOutcome.NotReviewed
            };

            await db.ExecuteAsync(
                """
                insert into evidence_submissions
                    (id, department_id, audit_item_id, file_name, extension, file_size, stored_path,
                     uploader_employee_id, uploader_name, uploaded_at,
                     agent_score, agent_defect_rate, agent_outcome, agent_findings, agent_checked_at,
                     provided_evidence, final_outcome, final_note,
                     reviewer_employee_id, reviewer_name, reviewed_at)
                values
                    (@id, @departmentId, @itemId, @fileName, @extension, @fileSize, @storedPath,
                     @uploaderId, @uploaderName, @uploadedAt,
                     null, null, @agentOutcome, null, null,
                     null, @finalOutcome, null,
                     null, null, null)
                """,
                cancellationToken,
                ("@id", submission.Id),
                ("@departmentId", submission.DepartmentId),
                ("@itemId", submission.AuditItemId),
                ("@fileName", submission.FileName),
                ("@extension", submission.Extension),
                ("@fileSize", submission.FileSize),
                ("@storedPath", submission.StoredPath),
                ("@uploaderId", submission.UploaderEmployeeId),
                ("@uploaderName", submission.UploaderName),
                ("@uploadedAt", LocalDb.Moment(submission.UploadedAt)),
                ("@agentOutcome", nameof(AgentOutcome.NotChecked)),
                ("@finalOutcome", nameof(FinalOutcome.NotReviewed)));

            // الرفع حدث يخص الجهة كلها، فيسجَّل في سجلها ويظهر في صفحة إشعارات ممثلها ومدققها.
            await NotifyAsync(
                upload.AuditItemId, NotificationKind.Uploaded, upload.FileName, null,
                upload.UploaderName, null, submission.UploadedAt, cancellationToken);

            return submission;
        }

        public Task<IReadOnlyList<EvidenceSubmission>> ListForDepartmentAsync(
            string departmentId, CancellationToken cancellationToken = default) =>
            FilesForDepartmentAsync(departmentId, cancellationToken);

        public async Task DeleteAsync(string submissionId, CancellationToken cancellationToken = default)
        {
            var storedPath = await db.FirstAsync(
                "select stored_path from evidence_submissions where id = @id",
                reader => reader.GetString(0), cancellationToken, ("@id", submissionId));

            if (storedPath is null)
                return;

            await db.ExecuteAsync(
                "delete from evidence_submissions where id = @id", cancellationToken, ("@id", submissionId));

            // المحو بعد حذف الصف: انقطاعٌ هنا يترك بايتات يتيمة على القرص، لا صفاً يقرأ ملفاً لا وجود له.
            await storage.DeleteAsync(storedPath, cancellationToken);
        }

        public async Task DeleteForItemAsync(int auditItemId, CancellationToken cancellationToken = default)
        {
            var keys = await db.QueryAsync(
                "select stored_path from evidence_submissions where audit_item_id = @itemId",
                reader => reader.GetString(0), cancellationToken, ("@itemId", auditItemId));

            if (keys.Count == 0)
                return;

            await db.ExecuteAsync(
                "delete from evidence_submissions where audit_item_id = @itemId",
                cancellationToken, ("@itemId", auditItemId));

            foreach (var key in keys)
                await storage.DeleteAsync(key, cancellationToken);
        }

        public Task InvalidateItemOutcomeAsync(int auditItemId, CancellationToken cancellationToken = default) =>
            db.ExecuteAsync(
                """
                update evidence_submissions
                set agent_outcome = @notChecked, agent_score = null, agent_defect_rate = null,
                    agent_findings = null, agent_checked_at = null,
                    final_outcome = @notReviewed, final_note = null,
                    reviewer_employee_id = null, reviewer_name = null, reviewed_at = null
                where audit_item_id = @itemId and agent_outcome <> @notChecked
                """,
                cancellationToken,
                ("@notChecked", nameof(AgentOutcome.NotChecked)),
                ("@notReviewed", nameof(FinalOutcome.NotReviewed)),
                ("@itemId", auditItemId));

        public Task SetItemEvidenceAsync(
            int auditItemId,
            IReadOnlyCollection<string> providedEvidence,
            CancellationToken cancellationToken = default) =>
            db.ExecuteAsync(
                "update evidence_submissions set provided_evidence = @text where audit_item_id = @itemId",
                cancellationToken,
                ("@text", providedEvidence.Count == 0 ? null : string.Join('\n', providedEvidence)),
                ("@itemId", auditItemId));

        public async Task ApplyAgentOutcomeAsync(
            int auditItemId, int score, string? findings, CancellationToken cancellationToken = default)
        {
            var outcome = AgentGate.IsApproved(score) ? AgentOutcome.Approved : AgentOutcome.Rejected;
            var checkedAt = DateTime.Now;

            await db.ExecuteAsync(
                """
                update evidence_submissions
                set agent_score = @score, agent_defect_rate = @defectRate, agent_outcome = @outcome,
                    agent_findings = @findings, agent_checked_at = @checkedAt
                where audit_item_id = @itemId
                """,
                cancellationToken,
                ("@score", score),
                ("@defectRate", 100 - score),
                ("@outcome", outcome.ToString()),
                ("@findings", findings),
                ("@checkedAt", LocalDb.Moment(checkedAt)),
                ("@itemId", auditItemId));

            await NotifyAsync(
                auditItemId,
                outcome == AgentOutcome.Approved ? NotificationKind.AgentPassed : NotificationKind.AgentFailed,
                null, score, null, null, checkedAt, cancellationToken);
        }

        public async Task<string> SkipAgentAuditAsync(
            int auditItemId, string actorName, string actorEmployeeId, CancellationToken cancellationToken = default)
        {
            // لا درجة تُكتب: انتحال درجة وكيل في سجل التدقيق كذب على رئيس الوحدة، والملاحظة تقول الحقيقة.
            var note = $"تجاوز يدوي لبوابة التدقيق الآلي: {actorName} (الرقم الوظيفي {actorEmployeeId}) " +
                       $"أجاز البند بلا فحص آلي بتاريخ {DateTime.Now:yyyy/MM/dd HH:mm}.";
            var checkedAt = DateTime.Now;

            await db.ExecuteAsync(
                """
                update evidence_submissions
                set agent_score = null, agent_defect_rate = null, agent_outcome = @outcome,
                    agent_findings = @note, agent_checked_at = @checkedAt
                where audit_item_id = @itemId
                """,
                cancellationToken,
                ("@outcome", nameof(AgentOutcome.Approved)),
                ("@note", note),
                ("@checkedAt", LocalDb.Moment(checkedAt)),
                ("@itemId", auditItemId));

            await NotifyAsync(
                auditItemId, NotificationKind.Overridden, null, null, actorName, note, checkedAt, cancellationToken);

            return note;
        }

        public async Task<byte[]?> ReadBytesAsync(string submissionId, CancellationToken cancellationToken = default)
        {
            var storedPath = await db.FirstAsync(
                "select stored_path from evidence_submissions where id = @id",
                reader => reader.GetString(0), cancellationToken, ("@id", submissionId));

            // الصف إن لم يكن فهو محذوف: تُقرأ null لا تستثني، فنقطة الملفات تردّ بأربعٍ لا وجدت.
            return storedPath is null ? null : await storage.GetAsync(storedPath, cancellationToken);
        }

        public Task<EvidenceFile?> GetFileAsync(string submissionId, CancellationToken cancellationToken = default) =>
            LoadFileAsync(submissionId, false, cancellationToken);

        public Task<EvidenceFile?> GetApprovedFileAsync(string submissionId, CancellationToken cancellationToken = default) =>
            LoadFileAsync(submissionId, true, cancellationToken);

        private async Task<EvidenceFile?> LoadFileAsync(
            string submissionId, bool approvedOnly, CancellationToken cancellationToken)
        {
            var submission = await db.FirstAsync(
                "select * from evidence_submissions where id = @id", Map, cancellationToken, ("@id", submissionId));

            // ملف لم يُعتمد بعد لا يُفتح لصاحب النتيجة، وغير الموجود لا حدث له: فلا رقم ملف يكشف بنداً ناقصاً.
            if (submission is null || (approvedOnly && submission.FinalOutcome != FinalOutcome.Approved))
                return null;

            var bytes = await storage.GetAsync(submission.StoredPath, cancellationToken);

            // بايتاتٌ لم ترجع = مفتاحٌ ممسوح من القرص: نفس الجواب الذي يردّ به الحذف قبل هذه القراءة.
            return bytes is null
                ? null
                : new EvidenceFile(bytes, submission.FileName, submission.Extension, submission.DepartmentId);
        }

        public async Task<IReadOnlyList<ReviewItemRow>> GetReviewBoardAsync(
            string departmentId, CancellationToken cancellationToken = default)
        {
            // الكتالوج أولاً ثم الملفات: البند الذي لم يجُز الوكيل ملفاته لا يصل إلى هذه الشاشة أصلاً.
            var items = await catalog.GetItemsAsync(departmentId, cancellationToken);
            var rows = await FilesForDepartmentAsync(departmentId, cancellationToken);

            var byItem = rows
                .GroupBy(s => s.AuditItemId)
                .ToDictionary(g => g.Key, g => g.Select(ToReviewRow).ToList());

            // الرمز هو ترتيب البند داخل جهته كما تعرضه لوحة الجهة، لا مترقيم الزرع العام.
            return items
                .Select(item =>
                {
                    var allFiles = byItem.TryGetValue(item.Id, out var found) ? found : [];
                    var passed = allFiles.Where(f => f.AgentOutcome == AgentOutcome.Approved).ToList();

                    return new ReviewItemRow(
                        item.Id,
                        item.Number,
                        item.Title,
                        item.IsoReference,
                        ClassifyForReview(passed.Select(f => f.FinalOutcome).ToList()),
                        passed,
                        allFiles.Count - passed.Count);
                })
                .ToList();
        }

        public async Task<IReadOnlyDictionary<int, ItemBoardFacts>> GetItemBoardFactsAsync(
            string departmentId, CancellationToken cancellationToken = default)
        {
            var files = await FilesForDepartmentAsync(departmentId, cancellationToken);
            var required = (await catalog.GetItemsAsync(departmentId, cancellationToken))
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
            // ملف لا يقبله الوكيل لا يُوقَّع، وملف غير موجود لا حدث له: فلا سطر إشعار بلا تغيير.
            var file = await db.FirstAsync(
                "select * from evidence_submissions where id = @id", Map, cancellationToken, ("@id", submissionId));

            if (file is null || file.AgentOutcome != AgentOutcome.Approved)
                return;

            var reviewedAt = DateTime.Now;

            await db.ExecuteAsync(
                """
                update evidence_submissions
                set final_outcome = @outcome, final_note = @note,
                    reviewer_employee_id = @reviewerId, reviewer_name = @reviewerName, reviewed_at = @reviewedAt
                where id = @id
                """,
                cancellationToken,
                ("@outcome", outcome.ToString()),
                ("@note", note),
                ("@reviewerId", reviewerEmployeeId),
                ("@reviewerName", reviewerName),
                ("@reviewedAt", LocalDb.Moment(reviewedAt)),
                ("@id", submissionId));

            await NotifyAsync(
                file.AuditItemId,
                outcome == FinalOutcome.Approved ? NotificationKind.Approved : NotificationKind.Rejected,
                file.FileName, null, reviewerName, note, reviewedAt, cancellationToken);
        }

        public async Task<IReadOnlyList<DepartmentAuditStat>> GetDepartmentStatsAsync(
            CancellationToken cancellationToken = default)
        {
            var departments = await catalog.GetDepartmentsAsync(cancellationToken);
            var files = await AllFilesAsync(cancellationToken);

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
                    (await catalog.GetItemsAsync(department.DepartmentId, cancellationToken)).Count,
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
            var departments = await catalog.GetDepartmentsAsync(cancellationToken);

            // ما أجازَه الوكيل وحده يدخل العدّ: فالبند المعتمد هو ما وقّعَ المدقّق كل ملفٍ وصلَه منه.
            var filesByItem = (await AgentApprovedFilesAsync(cancellationToken))
                .GroupBy(s => s.AuditItemId)
                .ToDictionary(g => g.Key, g => g
                    .OrderBy(s => s.UploadedAt).ThenBy(s => s.Id, StringComparer.Ordinal)
                    .Select(ToReviewRow).ToList());

            var approved = new List<ApprovedItemRow>();
            foreach (var department in departments)
            {
                foreach (var item in await catalog.GetItemsAsync(department.DepartmentId, cancellationToken))
                {
                    // لا بند بلا ملف مجاز، ولا بند بقيت منه ملفاً تنتظر توقيعاً أو رُفضت: الإغلاق يعني «مقبول» كلها.
                    if (!filesByItem.TryGetValue(item.Id, out var files)
                        || !files.All(f => f.FinalOutcome == FinalOutcome.Approved))
                        continue;

                    approved.Add(new ApprovedItemRow(
                        department.DepartmentId,
                        department.DepartmentName,
                        item.Id,
                        item.Number,
                        item.Title,
                        item.IsoReference,
                        files));
                }
            }

            return approved;
        }

        /// <summary>
        /// سطر إشعار عن بند، بعد أن يُقرأ عنوانه واسم جهته من صفيهما: العنوان والجهة يُحفظان في
        /// السطر كما هما لحظة الحدث، فلا يتبع سطرُ السجل تغييراً لاحقاً في الكتالوج.
        ///
        /// والسطر كله كتابة خلفية: فسقوطه لا يُسقط القرار الذي كُتب قبله، ولا يقول لصاحب الشاشة
        /// «لم يُحفظ» وهو محفوظ.
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
            try
            {
                var item = await db.FirstAsync(
                    """
                    select i.title, i.department_id, d.department_name
                    from audit_items i
                    join departments d on d.department_id = i.department_id
                    where i.id = @id
                    """,
                    reader => new NotificationSubject(reader.GetString(0), reader.GetString(1), reader.GetString(2)),
                    cancellationToken, ("@id", auditItemId));

                if (item is null)
                    return;

                await notifications.RecordAsync(new Notification
                {
                    Kind = kind,
                    DepartmentId = item.DepartmentId,
                    DepartmentName = item.DepartmentName,
                    ItemTitle = item.Title,
                    FileName = fileName,
                    ActorName = actorName,
                    AgentScore = score,
                    Note = note,
                    CreatedAt = createdAt
                }, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "تعذّر كتابة سطر إشعار عن البند {ItemId}", auditItemId);
            }
        }

        private async Task<IReadOnlyList<EvidenceSubmission>> FilesForDepartmentAsync(
            string departmentId, CancellationToken cancellationToken) =>
            await db.QueryAsync(
                """
                select * from evidence_submissions
                where department_id = @departmentId
                order by audit_item_id, uploaded_at, id
                """,
                Map, cancellationToken, ("@departmentId", departmentId));

        private Task<List<EvidenceSubmission>> AllFilesAsync(CancellationToken cancellationToken) =>
            db.QueryAsync("select * from evidence_submissions", Map, cancellationToken);

        private Task<List<EvidenceSubmission>> AgentApprovedFilesAsync(CancellationToken cancellationToken) =>
            db.QueryAsync(
                "select * from evidence_submissions where agent_outcome = @outcome",
                Map, cancellationToken, ("@outcome", nameof(AgentOutcome.Approved)));

        private static EvidenceSubmission Map(SqliteDataReader reader) => new()
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            DepartmentId = reader.GetString(reader.GetOrdinal("department_id")),
            AuditItemId = reader.GetInt32(reader.GetOrdinal("audit_item_id")),
            FileName = reader.GetString(reader.GetOrdinal("file_name")),
            Extension = reader.GetString(reader.GetOrdinal("extension")),
            FileSize = reader.GetInt64(reader.GetOrdinal("file_size")),
            StoredPath = reader.GetString(reader.GetOrdinal("stored_path")),
            UploaderEmployeeId = reader.GetString(reader.GetOrdinal("uploader_employee_id")),
            UploaderName = reader.GetString(reader.GetOrdinal("uploader_name")),
            UploadedAt = LocalDb.ReadMoment(reader.GetString(reader.GetOrdinal("uploaded_at"))),
            AgentScore = LocalDb.Int(reader, "agent_score"),
            AgentDefectRate = LocalDb.Int(reader, "agent_defect_rate"),
            AgentOutcome = Enum.Parse<AgentOutcome>(reader.GetString(reader.GetOrdinal("agent_outcome"))),
            AgentFindings = LocalDb.Text(reader, "agent_findings"),
            AgentCheckedAt = LocalDb.ReadMomentOrNull(LocalDb.Text(reader, "agent_checked_at")),
            ProvidedEvidence = LocalDb.Text(reader, "provided_evidence"),
            FinalOutcome = Enum.Parse<FinalOutcome>(reader.GetString(reader.GetOrdinal("final_outcome"))),
            FinalNote = LocalDb.Text(reader, "final_note"),
            ReviewerEmployeeId = LocalDb.Text(reader, "reviewer_employee_id"),
            ReviewerName = LocalDb.Text(reader, "reviewer_name"),
            ReviewedAt = LocalDb.ReadMomentOrNull(LocalDb.Text(reader, "reviewed_at"))
        };

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

        private sealed record NotificationSubject(string Title, string DepartmentId, string DepartmentName);

        private sealed record ItemFacts(
            IReadOnlyList<(AgentOutcome Agent, FinalOutcome Final, string? Provided)> Files,
            IReadOnlyList<string> RequiredEvidence);
    }
}
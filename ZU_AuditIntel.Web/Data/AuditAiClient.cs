using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>قسم AuditAi في appsettings.json: الرابط المحلي أو رابط ngrok بلا تعديل كود.</summary>
    public sealed class AuditAiOptions
    {
        public const string SectionName = "AuditAi";

        public string BaseUrl { get; set; } = "http://127.0.0.1:8000";
        public int TimeoutSeconds { get; set; } = 25;
        public int PollIntervalSeconds { get; set; } = 2;
        public int PollBudgetSeconds { get; set; } = 300;
    }

    /// <summary>
    /// حكم الوكيل على بند واحد. الأسماء snake_case لأنها عقدة العقد مع سيرفر الوكلاء ببايثون،
    /// وReadFromJsonAsync الافتراضية تقرأ camelCase فلو لم تُربط الأسماء صراحةً لصارت الحقول فارغة.
    /// </summary>
    public sealed record AiVerdict(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("score")] int Score,
        [property: JsonPropertyName("error_percentage")] double ErrorPercentage,
        [property: JsonPropertyName("short_note")] string? ShortNote,
        [property: JsonPropertyName("evaluation_note")] string? EvaluationNote,
        [property: JsonPropertyName("findings")] List<string>? Findings);

    /// <summary>حالة المهمة كما يعيدها السيرفر: queued ثم running ثم done أو failed.</summary>
    public sealed record AiJobSnapshot(
        [property: JsonPropertyName("job_id")] string JobId,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("progress")] double Progress,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("result")] AiVerdict? Result,
        [property: JsonPropertyName("error")] string? Error);

    /// <summary>
    /// جسر اللوحة إلى سيرفر الوكلاء: الإرسال يعود برقم مهمة في لحظات، ثم تُستطلع الحالة كل دورين.
    /// البديل القديم كان طلباً واحداً يمسك الاتصال أربع دقائق، فتبقى الشاشة بلا مؤشر ولا سبيل لإلغائه.
    /// </summary>
    public sealed class AuditAiClient(
        IHttpClientFactory httpFactory,
        IOptions<AuditAiOptions> options,
        ILogger<AuditAiClient> logger)
    {
        public const string ClientName = "AiAgentClient";

        public AuditAiOptions Options { get; } = options.Value;

        public async Task<string> SubmitJobAsync(
            string departmentId,
            int itemNumber,
            string itemTitle,
            string? isoReference,
            string? workflowText,
            IReadOnlyList<string> requiredEvidence,
            IReadOnlyList<AuditAiFilePayload> files,
            CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent
            {
                { new StringContent(departmentId), "department_id" },
                { new StringContent(itemNumber.ToString()), "item_number" },
                { new StringContent(itemTitle), "item_title" },
                { new StringContent(isoReference ?? string.Empty), "iso_reference" },
                { new StringContent(workflowText ?? string.Empty), "workflow_text" },
                { new StringContent(JsonSerializer.Serialize(requiredEvidence)), "required_evidence_json" }
            };

            foreach (var (name, contentType, bytes) in files)
            {
                var part = new ByteArrayContent(bytes);
                part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                form.Add(part, "files", name);
            }

            using var response = await Http().PostAsync("/api/jobs", form, cancellationToken);
            await EnsureReadAsync(response, cancellationToken);

            var snapshot = await response.Content.ReadFromJsonAsync<AiJobSnapshot>(cancellationToken)
                ?? throw new InvalidOperationException("عاد سيرفر الوكلاء بلا مهمة معروفة.");

            return snapshot.JobId;
        }

        public async Task<AiJobSnapshot> PollJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            using var response = await Http()
                .GetAsync($"/api/jobs/{Uri.EscapeDataString(jobId)}", cancellationToken);
            await EnsureReadAsync(response, cancellationToken);

            return await response.Content.ReadFromJsonAsync<AiJobSnapshot>(cancellationToken)
                ?? throw new InvalidOperationException("عاد سيرفر الوكلاء بلا حالة مهمة.");
        }

        /// <summary>
        /// الرفض البشري يُبلَّغ إلى ذاكرة الوكلاء ليُرفض الملف نفسه حتمياً في الفحص القادم. النداء
        /// خلفي لا ينتظره المدقق: قراره محفوظ في القاعدة على كل حال، والذاكرة قيدٌ إضافي على الفحص
        /// الآلي، ففشلها تحذيرٌ في السجل لا خطأٌ على الشاشة ولا إبطالاً لرفض.
        /// </summary>
        public async Task ReportHumanRejectionAsync(HumanRejectionFeedback feedback)
        {
            try
            {
                using var response = await Http().PostAsJsonAsync("/feedback", feedback);
                if (response.IsSuccessStatusCode)
                    return;

                var detail = await response.Content.ReadAsStringAsync();
                logger.LogWarning(
                    "لم تُسجَّل ذاكرة الرفض البشري ({Status}): {Detail}", (int)response.StatusCode, detail);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "تعذّر إرسال الرفض البشري إلى {Url}/feedback.", Options.BaseUrl);
            }
        }

        private HttpClient Http() => httpFactory.CreateClient(ClientName);

        /// <summary>جسم الخطأ من بايثون يشرح السبب (ملف كبير، نوع مرفوض، قاعدة مفقودة) فبدونه يتعمى العطل.</summary>
        private static async Task EnsureReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
                return;

            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"رفض سيرفر الوكلاء الطلب ({(int)response.StatusCode} {response.StatusCode}): {detail}");
        }
    }

    /// <summary>ملف دليل كما يُسلَّم إلى الوكيل: اسمه ونوع المحتوى وبايتاته المقروءة من المخزن.</summary>
    public sealed record AuditAiFilePayload(string Name, string ContentType, byte[] Bytes);

    /// <summary>
    /// رفض مدققٍ بشري كما يُخزَّن في ذاكرة الوكلاء: بصمة SHA-256 على بايتات الملف نفسها التي فحصها
    /// الوكيل، والبند وأدلته المعلَنة وسبب الرفض بلغة المدقق. الأسماء snake_case لعقد بايثون.
    /// </summary>
    public sealed record HumanRejectionFeedback(
        [property: JsonPropertyName("department_id")] string DepartmentId,
        [property: JsonPropertyName("item_number")] int ItemNumber,
        [property: JsonPropertyName("item_title")] string ItemTitle,
        [property: JsonPropertyName("iso_reference")] string IsoReference,
        [property: JsonPropertyName("evidence")] IReadOnlyList<string> Evidence,
        [property: JsonPropertyName("file_name")] string FileName,
        [property: JsonPropertyName("sha256")] string Sha256,
        [property: JsonPropertyName("reason")] string Reason,
        [property: JsonPropertyName("reviewer_name")] string ReviewerName,
        [property: JsonPropertyName("reviewer_employee_id")] string ReviewerEmployeeId,
        [property: JsonPropertyName("submission_id")] string SubmissionId);
}

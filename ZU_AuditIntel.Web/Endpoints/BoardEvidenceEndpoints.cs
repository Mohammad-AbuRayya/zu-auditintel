using ZU_AuditIntel.Web.Data;

namespace ZU_AuditIntel.Web.Endpoints
{
    /// <summary>
    /// تنزيل ممثل الجهة ملفاته التي رفعها على بنود جهته، ليقرأ ما أجازَه الوكيل وما رُفض قبل أن يعيد رفعه.
    /// الحق هنا هو حق فتح اللوحة نفسها: جهةٌ ملفاتها، أو مدير النظام للمعاينة، فلا رقم ملف يفتح ملف جهة أخرى.
    /// </summary>
    public static class BoardEvidenceEndpoints
    {
        public static IEndpointRouteBuilder MapBoardEvidenceEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/audit/evidence/{submissionId:int}", OpenAsync)
                .RequireAuthorization(AuthorizationPolicies.CanAudit);

            return app;
        }

        private static async Task<IResult> OpenAsync(
            int submissionId,
            HttpContext http,
            IEvidenceStore store,
            bool download = false)
        {
            var file = await store.GetFileRefAsync(submissionId);
            if (file is null || !http.User.CanViewBoard(file.DepartmentId))
                return Results.NotFound();

            var contentType = file.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            // الاسم الأصلي يُرسَل مع التنزيل فقط، فغيره يجعل المتصفح ينزّل الـ PDF بدل عرضه في تاب.
            return Results.File(file.FullPath, contentType, download ? file.FileName : null, enableRangeProcessing: true);
        }
    }
}

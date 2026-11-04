using ZU_AuditIntel.Web.Data;

namespace ZU_AuditIntel.Web.Endpoints
{
    public static class ReviewEndpoints
    {
        /// <summary>
        /// فتح ملف الدليل في تاب جديد. القراءة للتدقيق النهائي حقٌّ لمن يوقّع النتيجة، فلا يُعرض
        /// الملف لصاحبه: المرفوض من الوكيل لا يصل إلى شاشة بشرية أصلاً. ومدقّق الجهة لا يفتح برقم
        /// الملف ملفَ جهة أخرى، فجهةُ الملف تُقارَن بجهته قبل أن تُرسَل بايتاته.
        /// </summary>
        public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/review/evidence/{submissionId:int}", OpenAsync)
                .RequireAuthorization(AuthorizationPolicies.CanReviewBoard);

            return app;
        }

        private static async Task<IResult> OpenAsync(
            int submissionId,
            HttpContext http,
            IEvidenceStore store,
            bool download = false)
        {
            var file = await store.GetFileRefAsync(submissionId);
            if (file is null)
                return Results.NotFound();

            // لا Forbid هنا: فالفرق بين «ليس لك» و«ليس موجوداً» سؤال لا يجوز أن يُجابه برقم ملف.
            if (!http.User.CanReviewBoard(file.DepartmentId))
                return Results.NotFound();

            var contentType = file.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            // الاسم الأصلي يُرسَل مع التنزيل فقط، وإلا صار المتصفح يُنزّل الـ PDF بدل عرضه في تاب.
            return Results.File(file.FullPath, contentType, download ? file.FileName : null, enableRangeProcessing: true);
        }
    }
}

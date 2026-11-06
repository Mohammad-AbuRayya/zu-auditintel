using ZU_AuditIntel.Web.Data;

namespace ZU_AuditIntel.Web.Endpoints
{
    /// <summary>
    /// ملفات النتيجة النهائية كما يفتحها صاحبها: لا جهة تقف أمامه لأن السجل كلُّ الجهات، لكن السقف
    /// هو الاعتماد نفسه — فلا يُفتَح من هنا ملفٌ لم يقل فيه المدقّق البشري «مقبولاً» بعد.
    /// </summary>
    public static class FinalResultEndpoints
    {
        public static IEndpointRouteBuilder MapFinalResultEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/results/evidence/{submissionId:regex(^[A-Za-z0-9_-]+$)}", OpenAsync)
                .RequireAuthorization(AuthorizationPolicies.CanReadResults);

            return app;
        }

        private static async Task<IResult> OpenAsync(
            string submissionId,
            HttpContext http,
            IEvidenceStore store,
            bool download = false)
        {
            // لا Forbid هنا: ما لم يُعتمد ليس هنا، والفرق بين «ليس لك» و«ليس موجوداً» سؤال لا يجوز
            // أن تُجيبه شاشةٌ لا تملك أن تعرف كم بنداً ما زال مفتوحاً في جامعة كاملة.
            var file = await store.GetApprovedFileAsync(submissionId);
            if (file is null)
                return Results.NotFound();

            var contentType = file.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            // الاسم الأصلي يُرسَل مع التنزيل فقط، فغيره يجعل المتصفح ينزّل الـ PDF بدل عرضه في تاب.
            return Results.File(file.Bytes, contentType, download ? file.FileName : null);
        }
    }
}

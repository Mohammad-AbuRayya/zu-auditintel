using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZU_AuditIntel.Web.Data;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Endpoints
{
    public static class AccountEndpoints
    {
        /// <summary>مدة صلاحية جلسة الدخول بعد كل نشاط.</summary>
        public const int SessionHours = 8;

        public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/account/login", LoginAsync)
                .AllowAnonymous()
                .RequireRateLimiting("account-login");

            app.MapGet("/account/staff-kind", StaffKindAsync)
                .AllowAnonymous()
                .RequireRateLimiting("staff-kind");

            app.MapPost("/account/change-password", ChangePasswordAsync)
                .RequireAuthorization()
                .RequireRateLimiting("account-login");

            app.MapPost("/account/logout", LogoutAsync).RequireAuthorization();

            return app;
        }

        private static async Task<IResult> LoginAsync(
            HttpContext http,
            IAccountDirectory directory,
            IConfiguration config,
            [FromForm] string? employeeId,
            [FromForm] string? password)
        {
            var user = await directory.AuthenticateAsync(employeeId ?? "", password ?? "", http.RequestAborted);
            if (user is null)
                return Results.Redirect("/login?error=invalid");

            // الكلمة الأولية لكل حساب مزروع منشورة في README العام، فعلى خادمٍ إنترنت لا يُقبل
            // صاحبُها حتى يستلمها هو. قائمةُ أرقام من البيئة تضبط من يُقبل، ومتى تُركت فارغة —
            // وهي حالة الجهاز المطوّر — بقي المسار بلا قيد.
            var allowlist = (config["Audit:LoginAllowlist"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (allowlist.Length > 0 && !allowlist.Contains(user.EmployeeId))
                return Results.Redirect("/login?error=invalid");

            var principal = AuditClaims.ToPrincipal(user);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, SessionProperties());

            // الحساب المزروع بكلمة أولية لا يصل إلى أي شاشة عمل قبل تغييرها.
            return user.MustChangePassword
                ? Results.Redirect("/account/change-password?forced=1")
                : Results.Redirect(principal.LandingUrl());
        }

        private static async Task<IResult> ChangePasswordAsync(
            HttpContext http,
            IAccountDirectory directory,
            [FromForm] string? newPassword,
            [FromForm] string? confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword || newPassword.Length < 8)
                return Results.Redirect("/account/change-password?error=weak");

            var userId = int.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await directory.FindByIdAsync(userId, http.RequestAborted);
            if (user is null)
                return Results.Redirect("/login?error=session");

            await directory.ChangePasswordAsync(user, newPassword, http.RequestAborted);

            // إعادة إصدار الكوكيز حتى يسقط ادعاء "تغيير كلمة المرور مطلوب" من الجلسة الحالية.
            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                AuditClaims.ToPrincipal(user),
                SessionProperties());

            return Results.Redirect(http.User.LandingUrl());
        }

        private static async Task LogoutAsync(HttpContext http)
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// صفة صاحب الرقم الوظيفي لصفحة استعادة كلمة المرور: كادر أكاديمي أم موظف. الرد لا يفرّق بين
        /// «موظف» و«لا وجود»: من لا رقم له في الجدول يُصنَّف بعدد خاناته كما كانت الصفحة تفعل وحدها،
        /// فلا تصير النقطة عدّاد حسابات، ويبقى رابط تبديل البوابة لمن يخطئه التصنيف.
        /// </summary>
        private static async Task<IResult> StaffKindAsync(
            IAccountDirectory directory,
            string? employeeId,
            CancellationToken cancellationToken)
        {
            var raw = (employeeId ?? "").Trim();
            if (raw.Length is < 2 or > 10 || raw.Any(c => !char.IsDigit(c)))
                return Results.NotFound();

            var kind = await directory.FindAdjectiveByEmployeeIdAsync(raw, cancellationToken) switch
            {
                StaffAdjective.AcademicStaff => "academic",
                StaffAdjective.Employee => "employee",
                // رقم لا يعرفه الجدول (أو قيمة لم تعد لها صفة): تصنيفُ طول الخانات كما كانت الصفحة تفعل وحدها.
                _ => raw.Length <= 5 ? "academic" : "employee"
            };

            return Results.Json(new { kind });
        }

        private static AuthenticationProperties SessionProperties() => new()
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(SessionHours)
        };
    }

    /// <summary>سياسات التفويض المستخدمة في شاشات التدقيق.</summary>
    public static class AuthorizationPolicies
    {
        public const string CanAudit = "CanAudit";

        /// <summary>التدقيق النهائي على ملفات الجهات: لرئيس وحدة التطوير وضمان الجودة وللمدير.</summary>
        public const string CanReview = "CanReview";

        /// <summary>شاشة تدقيق جهة: تضاف إليها حق مدقّق الجهة اليدوي في صفحته هو.</summary>
        public const string CanReviewBoard = "CanReviewBoard";

        /// <summary>صفحة النتيجة النهائية: لقارئ النتيجة ولمدير النظام، ولا مدقّق فيها.</summary>
        public const string CanReadResults = "CanReadResults";
    }
}

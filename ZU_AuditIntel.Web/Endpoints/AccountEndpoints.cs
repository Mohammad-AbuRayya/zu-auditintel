using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZU_AuditIntel.Web.Data;

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

            app.MapPost("/account/change-password", ChangePasswordAsync)
                .RequireAuthorization()
                .RequireRateLimiting("account-login");

            app.MapPost("/account/logout", LogoutAsync).RequireAuthorization();

            return app;
        }

        private static async Task<IResult> LoginAsync(
            HttpContext http,
            IAccountDirectory directory,
            [FromForm] string? employeeId,
            [FromForm] string? password)
        {
            var user = await directory.AuthenticateAsync(employeeId ?? "", password ?? "", http.RequestAborted);
            if (user is null)
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
    }
}

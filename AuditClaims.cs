using System.Security.Claims;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web
{
    /// <summary>الادعاءات التي يحملها مبدأ الدخول، ودوال قراءتها من ClaimsPrincipal.</summary>
    public static class AuditClaims
    {
        public const string EmployeeId = "employee_id";
        public const string Department = "department";
        public const string MustChangePassword = "password_change_required";

        public static ClaimsPrincipal ToPrincipal(User user)
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(EmployeeId, user.EmployeeId),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim(MustChangePassword, user.MustChangePassword.ToString().ToLowerInvariant())
                ],
                "Cookies");

            if (user.DepartmentId is not null)
                identity.AddClaim(new Claim(Department, user.DepartmentId));

            return new ClaimsPrincipal(identity);
        }

        public static string? DepartmentId(this ClaimsPrincipal user) => user.FindFirstValue(Department);

        public static bool IsEntityRepresentative(this ClaimsPrincipal user) =>
            user.IsInRole(AuditorRole.EntityRepresentative.ToString());

        public static bool IsSystemAdministrator(this ClaimsPrincipal user) =>
            user.IsInRole(AuditorRole.SystemAdministrator.ToString());

        /// <summary>المسؤول عن المسؤولين: من يفتح شاشة التدقيق النهائي على ملفات كل الجهات.</summary>
        public static bool IsQualityHead(this ClaimsPrincipal user) =>
            user.IsInRole(AuditorRole.QualityHead.ToString());

        /// <summary>من يفتح شاشة التدقيق النهائي: رئيس الوحدة ومدير النظام (وضع الاختبار الإداري).</summary>
        public static bool IsFinalReviewer(this ClaimsPrincipal user) =>
            user.IsQualityHead() || user.IsSystemAdministrator();

        /// <summary>مدقّق جهة واحدة: صاحب شاشة تدقيق نهائية خاصة بجهته، لا شاشة له على بقية الجهات.</summary>
        public static bool IsEntityAuditor(this ClaimsPrincipal user) =>
            user.IsInRole(AuditorRole.EntityAuditor.ToString());

        /// <summary>جهة المدقّق اليدوي، أو null لغيره ولمن يعمل على كل الجهات من شاشة واحدة.</summary>
        public static string? AuditedDepartmentId(this ClaimsPrincipal user) =>
            user.IsEntityAuditor() ? user.DepartmentId() : null;

        /// <summary>
        /// صاحب النتيجة النهائية: يقرأ ما اعتمده المدقّق البشري من كل الجهات، ولا يملك تدقيقاً ولا توقيعاً.
        /// مدير النظام يدخل بشاشة القارئ كما يدخل بقية الشاشات بصفته لا باسم أصحابها.
        /// </summary>
        public static bool IsFinalResultReader(this ClaimsPrincipal user) =>
            user.IsInRole(AuditorRole.FinalResultReader.ToString());

        public static bool CanReadFinalResults(this ClaimsPrincipal user) =>
            user.IsFinalResultReader() || user.IsSystemAdministrator();

        /// <summary>
        /// من يفتح شاشة تدقيق جهة: رئيس الوحدة والمدير على أي جهة، ومدقّق الجهة على جهته وحدها.
        /// على الشاشة غير المقيّدة بجهة (جدول كل الجهات) يبقى الحق لمن يرى الكل.
        /// </summary>
        public static bool CanReviewBoard(this ClaimsPrincipal user, string? departmentId) =>
            user.IsFinalReviewer()
            || (user.IsEntityAuditor() && departmentId is not null && user.DepartmentId() == departmentId);

        /// <summary>
        /// لوحة جهة يفتحها ممثلُ تلك الجهة حصراً. مدير النظام استثناء مقصود للمعاينة والدعم،
        /// وهو وحده من يُعفى من تطابق الجهة حتى لا يحتاج حساب خارق لكل شاشة.
        /// </summary>
        public static bool CanViewBoard(this ClaimsPrincipal user, string departmentId) =>
            user.IsSystemAdministrator()
            || (user.IsEntityRepresentative() && user.DepartmentId() == departmentId);

        /// <summary>الصفحة التي يفتحها النظام بعد الدخول الناجح.</summary>
        public static string LandingUrl(this ClaimsPrincipal user)
        {
            // رئيس وحدة الجودة يعمل على ملفات كل الجهات، فلا لوحة جهة واحدة تُفتح له.
            if (user.IsQualityHead())
                return "/review";

            // مدقّق الجهة اليدوي لا شاشة له غير صفحة جهته، فلا معنى لأن يهبط على صفحة الانتظار.
            var audited = user.AuditedDepartmentId();
            if (audited is not null)
                return $"/review/{audited}";

            // ممثل الجهة يفتح لوحة تدقيق جهته، ومن لا جهة له أو لا لوحة لها يذهب إلى صفحة الانتظار.
            var departmentId = user.DepartmentId();
            return user.IsEntityRepresentative() && departmentId is not null
                ? $"/Audit/{departmentId}"
                : "/pending";
        }
    }
}

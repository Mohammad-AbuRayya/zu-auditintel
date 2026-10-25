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

        /// <summary>التسمية العربية للدور كما تظهر في شريط الهوية.</summary>
        public static string RoleLabel(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role) switch
        {
            nameof(AuditorRole.LiaisonOfficer) => "ضابط الارتباط / المدقق الداخلي",
            nameof(AuditorRole.QualityHead) => "رئيس وحدة الاعتماد وضمان الجودة",
            nameof(AuditorRole.EntityRepresentative) => "ممثل الجهة",
            _ => "زائر"
        };

        /// <summary>الصفحة التي يفتحها النظام بعد الدخول الناجح.</summary>
        public static string LandingUrl(this ClaimsPrincipal user)
        {
            // لوحة المكتبة هي الكتالوج المعتمد وحدها، ولممثل المكتبة حصراً؛ غير ذلك إلى صفحة الانتظار.
            return user.IsEntityRepresentative() && user.DepartmentId() == DepartmentIds.Library
                ? "/LibraryAudit"
                : "/pending";
        }
    }
}

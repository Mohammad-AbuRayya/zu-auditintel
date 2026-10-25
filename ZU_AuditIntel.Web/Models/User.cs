namespace ZU_AuditIntel.Web.Models
{
    public enum AuditorRole
    {
        /// <summary>مسؤول الجهة: يرفع أدلة بنود جهته فقط.</summary>
        EntityRepresentative,

        /// <summary>ضابط الارتباط/المدقق البشري: راجع ملفات الجهة بعد اجتياز الفحص الآلي.</summary>
        LiaisonOfficer,

        /// <summary>رئيس وحدة التطوير وضمان الجودة: الاعتماد النهائي على مستوى الجامعة.</summary>
        QualityHead,

        /// <summary>
        /// حساب إداري واحد يفتح كل لوحات الجهات لأغراض المعاينة والدعم. لا يرفع أدلة ولا يوقّع
        /// على نتيجة تدقيق: صلاحيته قراءة الشاشات، لا الحلول محل الأدوار الثلاثة في المسار.
        /// </summary>
        SystemAdministrator
    }

    /// <summary>حساب مستخدم النظام، مربوط بالرقم الوظيفي وبالجهة عند الحاجة.</summary>
    public class User
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = "";
        public string FullName { get; set; } = "";
        public AuditorRole Role { get; set; }

        /// <summary>الجهة التي يخدمها الحساب؛ null لرئيس وحدة الجودة لأنه على مستوى الجامعة.</summary>
        public string? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public string PasswordSalt { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public int PasswordIterations { get; set; }
        public bool MustChangePassword { get; set; }
    }
}

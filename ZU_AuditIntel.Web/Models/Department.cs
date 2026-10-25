namespace ZU_AuditIntel.Web.Models
{
    public class Department
    {
        public string DepartmentId { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public List<AuditItem> AuditItems { get; set; } = new();
    }

    /// <summary>معرّفات الجهات الثابتة المستخدمة في الزرع وفي شاشات التدقيق.</summary>
    public static class DepartmentIds
    {
        public const string HealthCenter = "health_center";
        public const string Admission = "admission";
        public const string Library = "library";
        public const string HumanResources = "hr";
        public const string Presidency = "presidency";
        public const string StudentAffairs = "student_affairs";
        public const string ItCenter = "it_center";
        public const string QualityUnit = "quality_unit";
        public const string Postgraduate = "postgraduate";
        public const string Faculties = "faculties";
        public const string ScientificResearch = "scientific_research";
        public const string Procurement = "procurement";
    }
}

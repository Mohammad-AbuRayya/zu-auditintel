namespace ZU_AuditIntel.Web.Models
{
    /// <summary>تحديد ما إذا كان البند مشتركاً بين الجهات أم خاصاً بالجهة المدققة.</summary>
    public enum AuditItemCategory
    {
        Common,
        DepartmentSpecific
    }

    /// <summary>
    /// تعريف بند تدقيق (بيانات كتالوج ثابتة). نتائج التدقيق التشغيلية ليست هنا: ملفات الأدلة وقرار
    /// الوكيل وقرار التدقيق النهائي تعيش في <see cref="EvidenceSubmission"/>.
    /// </summary>
    public class AuditItem
    {
        public int Id { get; set; }
        public string DepartmentId { get; set; } = "";
        public Department Department { get; set; } = null!;
        public AuditItemCategory Category { get; set; }
        public string Title { get; set; } = "";
        public string IsoReference { get; set; } = "";
        public string Workflow { get; set; } = "";
        public int SortOrder { get; set; }

        public List<AuditQuestion> Questions { get; set; } = new();
        public List<AuditEvidenceRequirement> EvidenceRequirements { get; set; } = new();
    }
}

namespace ZU_AuditIntel.Web.Models
{
    /// <summary>الأدلة المستندية المطلوبة لإثبات مطابقة بند التدقيق.</summary>
    public class AuditEvidenceRequirement
    {
        public int Id { get; set; }
        public int AuditItemId { get; set; }
        public AuditItem AuditItem { get; set; } = null!;
        public string Text { get; set; } = "";
        public int SortOrder { get; set; }
    }
}

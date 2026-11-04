namespace ZU_AuditIntel.Web.Models
{
    /// <summary>سؤال مطابقة يُطرح على الجهة عن بند التدقيق.</summary>
    public class AuditQuestion
    {
        public int Id { get; set; }
        public int AuditItemId { get; set; }
        public AuditItem AuditItem { get; set; } = null!;
        public string Text { get; set; } = "";
        public int SortOrder { get; set; }
    }
}

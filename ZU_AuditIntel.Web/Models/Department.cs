namespace ZU_AuditIntel.Web.Models
{
    public class Department
    {
        public string DepartmentId { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public List<AuditItem> AuditItems { get; set; } = new();
    }
}
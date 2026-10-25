namespace ZU_AuditIntel.Web.Models
{
    public class AuditItem
    {
        public int Id { get; set; }
        public string DepartmentId { get; set; } = "";
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";

        // الخصائص الجديدة لتوثيق الآيزو ومسار العمل
        public string IsoReference { get; set; } = "";
        public string WorkflowJson { get; set; } = "";

        public string QuestionsJson { get; set; } = "";
        public string EvidencesJson { get; set; } = "";

        public string Status { get; set; } = "بانتظار الرفع";

        public List<string> GetWorkflow() => string.IsNullOrEmpty(WorkflowJson) ? new() : WorkflowJson.Split("||").ToList();
        public List<string> GetQuestions() => string.IsNullOrEmpty(QuestionsJson) ? new() : QuestionsJson.Split("||").ToList();
        public List<string> GetEvidences() => string.IsNullOrEmpty(EvidencesJson) ? new() : EvidencesJson.Split("||").ToList();
    }
}
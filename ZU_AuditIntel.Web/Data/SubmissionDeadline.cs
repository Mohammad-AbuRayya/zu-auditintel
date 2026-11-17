namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// آخر موعد للتسليم، واحدٌ لكل الجهات. اليوم المحدّد مشمولٌ كله: يُخزَّن آخر لحظةٍ منه،
    /// فالجهة ترفع حتى منتصف ليله لا حتى أوله. وغياب الموعد يعني «غير محدد» فالرفع مفتوح.
    /// </summary>
    public sealed record SubmissionDeadline(
        DateTime? DueAt,
        string SetByName,
        string SetByEmployeeId,
        DateTime? SetAt)
    {
        public static SubmissionDeadline None { get; } = new(null, "", "", null);

        public bool IsSet => DueAt is not null;

        /// <summary>انتهت المهلة: الوقت الحالي تجاوز الموعد، فيُقفل الرفع في كل صفحات الجهات.</summary>
        public bool IsExpired => DueAt is { } due && DateTime.Now > due;
    }

    /// <summary>
    /// رفعٌ وصل بعد انتهاء المهلة. يُرمى من المخزن نفسه لا من الواجهة وحدها، فزرٌ أُعيد إظهاره
    /// بأدوات المتصفح أو جلسةٌ فُتحت قبل انتهاء المهلة لا يكتبان ملفاً بعد الموعد.
    /// </summary>
    public sealed class SubmissionClosedException(DateTime dueAt) : Exception
    {
        public DateTime DueAt { get; } = dueAt;
    }

    public interface ISubmissionDeadlineStore
    {
        Task<SubmissionDeadline> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>يكتب الموعد أو يمسحه (null = بلا موعد)، ويترك اسم من حدّده ووقته أثرَ القرار.</summary>
        Task SetAsync(
            DateTime? dueAt,
            string employeeId,
            string name,
            CancellationToken cancellationToken = default);
    }
}
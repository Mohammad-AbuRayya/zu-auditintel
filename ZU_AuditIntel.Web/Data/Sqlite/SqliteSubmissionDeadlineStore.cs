namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// موعد التسليم في القاعدة المحلية: صفٌ واحد بمفتاحٍ ثابت، فكتابته استبدالٌ لا إضافة،
    /// وقراءته سطرٌ واحد بلا فهرس. وغياب الصف حالةٌ مشروعة تعني «غير محدد».
    /// </summary>
    public sealed class SqliteSubmissionDeadlineStore(LocalDb db) : ISubmissionDeadlineStore
    {
        public async Task<SubmissionDeadline> GetAsync(CancellationToken cancellationToken = default)
        {
            var row = await db.FirstAsync(
                "select due_at, set_by_name, set_by_employee_id, set_at from submission_deadline where id = 1",
                reader => new SubmissionDeadline(
                    LocalDb.ReadMomentOrNull(LocalDb.Text(reader, "due_at")),
                    LocalDb.Text(reader, "set_by_name") ?? "",
                    LocalDb.Text(reader, "set_by_employee_id") ?? "",
                    LocalDb.ReadMomentOrNull(LocalDb.Text(reader, "set_at"))),
                cancellationToken);

            return row ?? SubmissionDeadline.None;
        }

        public Task SetAsync(
            DateTime? dueAt, string employeeId, string name, CancellationToken cancellationToken = default)
            => db.ExecuteAsync(
                """
                insert into submission_deadline (id, due_at, set_by_employee_id, set_by_name, set_at)
                values (1, @dueAt, @employeeId, @name, @setAt)
                on conflict(id) do update set
                    due_at = excluded.due_at,
                    set_by_employee_id = excluded.set_by_employee_id,
                    set_by_name = excluded.set_by_name,
                    set_at = excluded.set_at
                """,
                cancellationToken,
                ("@dueAt", dueAt is null ? null : LocalDb.Moment(dueAt.Value)),
                ("@employeeId", employeeId),
                ("@name", name),
                ("@setAt", LocalDb.Moment(DateTime.Now)));
    }
}
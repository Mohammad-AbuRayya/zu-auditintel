using Microsoft.Data.Sqlite;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// دليل الحسابات على القاعدة المحلية: مفتاح الصف هو الرقم الوظيفي نفسه، فالدخول قراءة مباشرة
    /// بمفتاح واحد بلا استعلام ولا فهرس مركّب. وتغيير الكلمة يكتب أربعة حقول فقط ولا يمسّ ما عداه،
    /// فتبقى بصمة صاحب الحساب كما هي ولا تُستبدل كلمةٌ محفوظة لأجل اختبار.
    /// </summary>
    public sealed class SqliteAccountDirectory(LocalDb db) : IAccountDirectory
    {
        public async Task<User?> AuthenticateAsync(
            string employeeId, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrEmpty(password))
                return null;

            var user = await LoadByEmployeeIdAsync(employeeId.Trim(), cancellationToken);

            // كلمة المرور الخاطئة تُعامل معاملة الرقم الوظيفي غير الموجود: لا تكشف أيّ الرقمين صحيح.
            if (user is null)
                return null;

            var stored = new HashedPassword(user.PasswordSalt, user.PasswordHash, user.PasswordIterations);
            return PasswordHasher.Verify(password, stored) ? user : null;
        }

        public async Task<StaffAdjective?> FindAdjectiveByEmployeeIdAsync(
            string employeeId, CancellationToken cancellationToken = default)
        {
            var normalized = employeeId.Trim();
            if (normalized.Length == 0)
                return null;

            return (await LoadByEmployeeIdAsync(normalized, cancellationToken))?.Adjective;
        }

        public Task<User?> FindByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            db.FirstAsync("select * from users where id = @id", Map, cancellationToken, ("@id", userId));

        public async Task ChangePasswordAsync(
            User user, string newPassword, CancellationToken cancellationToken = default)
        {
            var hashed = PasswordHasher.Hash(newPassword);

            await db.ExecuteAsync(
                """
                update users
                set password_salt = @salt, password_hash = @hash,
                    password_iterations = @iterations, must_change_password = 0
                where employee_id = @employeeId
                """,
                cancellationToken,
                ("@salt", hashed.Salt),
                ("@hash", hashed.Hash),
                ("@iterations", hashed.Iterations),
                ("@employeeId", user.EmployeeId));

            user.PasswordSalt = hashed.Salt;
            user.PasswordHash = hashed.Hash;
            user.PasswordIterations = hashed.Iterations;
            user.MustChangePassword = false;
        }

        /// <summary>
        /// الزرع متكرّر الآمن: تُقرأ مفاتيح الموجود مرة، فلا تلمس الأداة حساباً غيّر صاحبه كلمته
        /// ولا حساباً استُبدل اسمه. وأرقام الأسطر مورثة، فالناقص يأخذ رقمه بالترتيب نفسه.
        /// </summary>
        public async Task SeedInitialUsersAsync(CancellationToken cancellationToken = default)
        {
            var existing = (await db.QueryAsync(
                "select employee_id from users", reader => reader.GetString(0), cancellationToken))
                .ToHashSet(StringComparer.Ordinal);

            var missing = SeedAccounts.All
                .Select((seed, index) => (Seed: seed, Id: index + 1))
                .Where(pair => !existing.Contains(pair.Seed.EmployeeId))
                .ToList();

            if (missing.Count == 0)
                return;

            await db.InTransactionAsync(async (connection, transaction, token) =>
            {
                foreach (var (seed, id) in missing)
                {
                    var hashed = PasswordHasher.Hash(seed.InitialPassword);

                    await using var command = LocalDb.Build(connection,
                        """
                        insert into users (employee_id, id, full_name, job_title, adjective, role,
                                           department_id, password_salt, password_hash,
                                           password_iterations, must_change_password)
                        values (@employeeId, @id, @fullName, @jobTitle, @adjective, @role,
                                @departmentId, @salt, @hash, @iterations, 1)
                        """,
                        [
                            ("@employeeId", seed.EmployeeId),
                            ("@id", id),
                            ("@fullName", seed.FullName),
                            ("@jobTitle", seed.JobTitle),
                            ("@adjective", SeedAccounts.ClassifyAdjective(seed.JobTitle).ToString()),
                            ("@role", seed.Role.ToString()),
                            ("@departmentId", seed.DepartmentId),
                            ("@salt", hashed.Salt),
                            ("@hash", hashed.Hash),
                            ("@iterations", hashed.Iterations)
                        ],
                        transaction);

                    await command.ExecuteNonQueryAsync(token);
                }
            }, cancellationToken);
        }

        private Task<User?> LoadByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken) =>
            employeeId.Length == 0
                ? Task.FromResult<User?>(null)
                : db.FirstAsync(
                    "select * from users where employee_id = @employeeId", Map, cancellationToken,
                    ("@employeeId", employeeId));

        internal static User Map(SqliteDataReader reader) => new()
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            EmployeeId = reader.GetString(reader.GetOrdinal("employee_id")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            JobTitle = reader.GetString(reader.GetOrdinal("job_title")),
            Adjective = Enum.Parse<StaffAdjective>(reader.GetString(reader.GetOrdinal("adjective"))),
            Role = Enum.Parse<AuditorRole>(reader.GetString(reader.GetOrdinal("role"))),
            DepartmentId = LocalDb.Text(reader, "department_id"),
            PasswordSalt = reader.GetString(reader.GetOrdinal("password_salt")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            PasswordIterations = reader.GetInt32(reader.GetOrdinal("password_iterations")),
            MustChangePassword = reader.GetInt32(reader.GetOrdinal("must_change_password")) != 0
        };
    }
}
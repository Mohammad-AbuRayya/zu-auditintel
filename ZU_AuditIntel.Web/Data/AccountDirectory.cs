using Microsoft.EntityFrameworkCore;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    public interface IAccountDirectory
    {
        Task<User?> AuthenticateAsync(string employeeId, string password, CancellationToken cancellationToken = default);
        Task<User?> FindByIdAsync(int userId, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(User user, string newPassword, CancellationToken cancellationToken = default);
        Task SeedInitialUsersAsync(CancellationToken cancellationToken = default);
    }

    public sealed class EfAccountDirectory(IDbContextFactory<AppDbContext> contextFactory) : IAccountDirectory
    {
        public async Task<User?> AuthenticateAsync(string employeeId, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrEmpty(password))
                return null;

            var normalized = employeeId.Trim();
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(
                u => u.EmployeeId == normalized, cancellationToken);

            // كلمة المرور الخاطئة تُعامل معاملة الرقم الوظيفي غير الموجود: لا تكشف أيّ الرقمين صحيح.
            if (user is null)
                return null;

            var stored = new HashedPassword(user.PasswordSalt, user.PasswordHash, user.PasswordIterations);
            return PasswordHasher.Verify(password, stored) ? user : null;
        }

        public async Task<User?> FindByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task ChangePasswordAsync(User user, string newPassword, CancellationToken cancellationToken = default)
        {
            var hashed = PasswordHasher.Hash(newPassword);
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            db.Attach(user);
            user.PasswordSalt = hashed.Salt;
            user.PasswordHash = hashed.Hash;
            user.PasswordIterations = hashed.Iterations;
            user.MustChangePassword = false;
            await db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// الحسابات الأولية لا تُزرع عبر HasData لأن الملح عشوائي، فثبات الترحيل يتكسّر.
        /// تُنشأ هنا عند الإقلاع بشكل متكرّر الآمن إن لم تكن موجودة.
        /// </summary>
        public async Task SeedInitialUsersAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            foreach (var seed in InitialAccounts)
            {
                if (await db.Users.AnyAsync(u => u.EmployeeId == seed.EmployeeId, cancellationToken))
                    continue;

                var hashed = PasswordHasher.Hash(seed.InitialPassword);
                db.Users.Add(new User
                {
                    EmployeeId = seed.EmployeeId,
                    FullName = seed.FullName,
                    Role = seed.Role,
                    DepartmentId = seed.DepartmentId,
                    PasswordSalt = hashed.Salt,
                    PasswordHash = hashed.Hash,
                    PasswordIterations = hashed.Iterations,
                    MustChangePassword = true
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        private sealed record InitialAccount(
            string EmployeeId,
            string FullName,
            AuditorRole Role,
            string? DepartmentId,
            string InitialPassword);

        // كلمات هذه الحسابات أولية مُعلنة في README ويُجبر النظام على تغييرها عند أول دخول.
        private static readonly InitialAccount[] InitialAccounts =
        [
            new("1000101", "مسؤول دائرة المكتبة العامة", AuditorRole.EntityRepresentative, DepartmentIds.Library, "Zu#Library1"),
            new("1000102", "مسؤول المركز الصحي", AuditorRole.EntityRepresentative, DepartmentIds.HealthCenter, "Zu#Health1"),
            new("2000201", "ضابط ارتباط جودة - المكتبة", AuditorRole.LiaisonOfficer, DepartmentIds.Library, "Zu#Liaison1"),
            new("3000301", "رئيس وحدة التطوير وضمان الجودة", AuditorRole.QualityHead, null, "Zu#Quality1")
        ];
    }
}

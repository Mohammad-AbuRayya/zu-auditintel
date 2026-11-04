using Microsoft.EntityFrameworkCore;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<User> Users => Set<User>();
        public DbSet<AuditItem> AuditItems => Set<AuditItem>();
        public DbSet<AuditQuestion> Questions => Set<AuditQuestion>();
        public DbSet<AuditEvidenceRequirement> EvidenceRequirements => Set<AuditEvidenceRequirement>();
        public DbSet<EvidenceSubmission> EvidenceSubmissions => Set<EvidenceSubmission>();

        /// <summary>ورقة أدلة البنود: سطر لكل بند، وعمود لكل وثيقة. الاسم في القاعدة <c>Evidence</c>.</summary>
        public DbSet<Evidence> EvidenceRows => Set<Evidence>();

        /// <summary>سجل ما جرى على ملفات الأدلة، ومعه قراءة كل حساب لإشعاراته.</summary>
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationRead> NotificationReads => Set<NotificationRead>();

        /// <summary>محادثات الحسابات بعضها مع بعض، ورسائل كل محادثة.</summary>
        public DbSet<MessageThread> MessageThreads => Set<MessageThread>();
        public DbSet<Message> Messages => Set<Message>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.DepartmentId);
                entity.Property(d => d.DepartmentId).HasMaxLength(50);
                entity.Property(d => d.DepartmentName).IsRequired().HasMaxLength(200);
                entity.HasMany(d => d.AuditItems)
                    .WithOne(i => i.Department)
                    .HasForeignKey(i => i.DepartmentId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.EmployeeId).IsUnique();
                entity.Property(u => u.EmployeeId).HasMaxLength(50).IsRequired();
                entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
                entity.Property(u => u.JobTitle).HasMaxLength(200).IsRequired();
                entity.Property(u => u.PasswordSalt).HasMaxLength(100).IsRequired();
                entity.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
                entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
                entity.Property(u => u.Adjective).HasConversion<string>().HasMaxLength(32);
                entity.HasOne(u => u.Department)
                    .WithMany()
                    .HasForeignKey(u => u.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AuditItem>(entity =>
            {
                entity.Property(i => i.DepartmentId).HasMaxLength(50);
                entity.Property(i => i.Title).IsRequired().HasMaxLength(300);
                entity.Property(i => i.IsoReference).HasMaxLength(50);
                entity.Property(i => i.Category).HasConversion<string>().HasMaxLength(32);
                entity.HasIndex(i => new { i.DepartmentId, i.SortOrder });

                entity.HasMany(i => i.Questions)
                    .WithOne(q => q.AuditItem)
                    .HasForeignKey(q => q.AuditItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(i => i.EvidenceRequirements)
                    .WithOne(e => e.AuditItem)
                    .HasForeignKey(e => e.AuditItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditQuestion>(entity =>
            {
                entity.Property(q => q.Text).IsRequired();
                entity.HasIndex(q => new { q.AuditItemId, q.SortOrder }).IsUnique();
            });

            modelBuilder.Entity<AuditEvidenceRequirement>(entity =>
            {
                entity.Property(e => e.Text).IsRequired();
                entity.HasIndex(e => new { e.AuditItemId, e.SortOrder }).IsUnique();
            });

            modelBuilder.Entity<Evidence>(entity =>
            {
                entity.ToTable("Evidence");

                // جدول مستقل: لا علاقة له بغيره ولا فهرس عليه. أعمدة النص بلا حدّ، فقد يصف
                // عمودٌ واحداً عشرة أدلة لبند واحد.
                entity.Property(e => e.DepartmentName).IsRequired();
                entity.Property(e => e.ItemTitle).IsRequired();
                entity.Property(e => e.InstructionsEvidence).IsRequired();
                entity.Property(e => e.InternalAuditEvidence).IsRequired();
                entity.Property(e => e.InstructionsWorkflow).IsRequired();
                entity.Property(e => e.InstructionsQuestions).IsRequired();
            });

            modelBuilder.Entity<EvidenceSubmission>(entity =>
            {
                entity.Property(s => s.DepartmentId).HasMaxLength(50).IsRequired();
                entity.Property(s => s.FileName).HasMaxLength(300).IsRequired();
                entity.Property(s => s.Extension).HasMaxLength(10).IsRequired();
                entity.Property(s => s.StoredPath).HasMaxLength(400).IsRequired();
                entity.Property(s => s.UploaderEmployeeId).HasMaxLength(50).IsRequired();
                entity.Property(s => s.UploaderName).HasMaxLength(200).IsRequired();
                entity.Property(s => s.AgentFindings).HasMaxLength(4000);
                entity.Property(s => s.ProvidedEvidence).HasMaxLength(4000);
                entity.Property(s => s.FinalNote).HasMaxLength(2000);
                entity.Property(s => s.ReviewerEmployeeId).HasMaxLength(50);
                entity.Property(s => s.ReviewerName).HasMaxLength(200);
                entity.Property(s => s.AgentOutcome).HasConversion<string>().HasMaxLength(20);
                entity.Property(s => s.FinalOutcome).HasConversion<string>().HasMaxLength(20);

                // اللوحة النهائية تُصفّي بالقرارين إياهما، والصفحة الجهوية تُجمَّع بالبند.
                entity.HasIndex(s => new { s.DepartmentId, s.AuditItemId });
                entity.HasIndex(s => new { s.AgentOutcome, s.FinalOutcome });

                entity.HasOne(s => s.Department)
                    .WithMany()
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.AuditItem)
                    .WithMany()
                    .HasForeignKey(s => s.AuditItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                // جدول سجل مستقل: لا علاقة له ببند أو ملف، فيبقى سطر الحدث بعد حذف ما تكلّم عنه.
                entity.Property(n => n.Kind).HasConversion<string>().HasMaxLength(20);
                entity.Property(n => n.DepartmentId).HasMaxLength(50).IsRequired();
                entity.Property(n => n.DepartmentName).HasMaxLength(200).IsRequired();
                entity.Property(n => n.ItemTitle).HasMaxLength(300).IsRequired();
                entity.Property(n => n.FileName).HasMaxLength(300);
                entity.Property(n => n.ActorName).HasMaxLength(200);
                entity.Property(n => n.Note).HasMaxLength(2000);

                // الصفحة تقرأ الأحدث أولاً، وجهة صاحب الحساب تضيّق القراءة دائماً.
                entity.HasIndex(n => n.CreatedAt);
                entity.HasIndex(n => n.DepartmentId);
            });

            modelBuilder.Entity<NotificationRead>(entity =>
            {
                entity.HasKey(r => new { r.NotificationId, r.EmployeeId });
                entity.Property(r => r.EmployeeId).HasMaxLength(50).IsRequired();
            });

            modelBuilder.Entity<MessageThread>(entity =>
            {
                // زوج الحسابين مفتاحٌ واحد: لا محادثتان بين الشخصين أنفسهما.
                entity.HasIndex(t => t.PairKey).IsUnique();
                entity.Property(t => t.PairKey).HasMaxLength(120).IsRequired();
                entity.Property(t => t.EmployeeIdA).HasMaxLength(50).IsRequired();
                entity.Property(t => t.EmployeeIdB).HasMaxLength(50).IsRequired();

                // القائمة تُرتَّب بالأحدث، وتُضيّق بحساب أحد الطرفين.
                entity.HasIndex(t => t.LastMessageAt);
                entity.HasIndex(t => t.EmployeeIdA);
                entity.HasIndex(t => t.EmployeeIdB);

                entity.HasMany(t => t.Messages)
                    .WithOne(m => m.Thread)
                    .HasForeignKey(m => m.ThreadId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.Property(m => m.SenderEmployeeId).HasMaxLength(50).IsRequired();
                entity.Property(m => m.SenderName).HasMaxLength(200).IsRequired();
                entity.Property(m => m.Body).HasMaxLength(2000).IsRequired();
                entity.HasIndex(m => new { m.ThreadId, m.SentAt });
            });

            modelBuilder.Entity<Department>().HasData(SeedData.Departments());
            modelBuilder.Entity<AuditItem>().HasData(SeedData.AuditItems());
            modelBuilder.Entity<AuditQuestion>().HasData(SeedData.Questions());
            modelBuilder.Entity<AuditEvidenceRequirement>().HasData(SeedData.EvidenceRequirements());
        }
    }
}

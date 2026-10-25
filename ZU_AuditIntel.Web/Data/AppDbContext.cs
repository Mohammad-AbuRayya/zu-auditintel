using Microsoft.EntityFrameworkCore;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<AuditItem> AuditItems => Set<AuditItem>();
        public DbSet<AuditQuestion> Questions => Set<AuditQuestion>();
        public DbSet<AuditEvidenceRequirement> EvidenceRequirements => Set<AuditEvidenceRequirement>();

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

            modelBuilder.Entity<Department>().HasData(SeedData.Departments());
            modelBuilder.Entity<AuditItem>().HasData(SeedData.AuditItems());
            modelBuilder.Entity<AuditQuestion>().HasData(SeedData.Questions());
            modelBuilder.Entity<AuditEvidenceRequirement>().HasData(SeedData.EvidenceRequirements());
        }
    }
}

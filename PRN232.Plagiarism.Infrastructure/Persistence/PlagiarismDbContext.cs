using Microsoft.EntityFrameworkCore;
using PRN232.Plagiarism.Domain.Entities;

namespace PRN232.Plagiarism.Infrastructure.Persistence;

public class PlagiarismDbContext : DbContext
{
    public PlagiarismDbContext(DbContextOptions<PlagiarismDbContext> options) : base(options)
    {
    }

    public DbSet<PlagiarismRecord> PlagiarismRecords => Set<PlagiarismRecord>();
    public DbSet<PlagiarismViolationRecord> PlagiarismViolationRecords => Set<PlagiarismViolationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình bảng PlagiarismRecord
        modelBuilder.Entity<PlagiarismRecord>(entity =>
        {
            entity.ToTable("PlagiarismRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StudentId).IsRequired().HasMaxLength(50);
            
            // Mối quan hệ 1-N với PlagiarismViolationRecord
            entity.HasMany(e => e.Violations)
                  .WithOne(v => v.PlagiarismRecord)
                  .HasForeignKey(v => v.PlagiarismRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Cấu hình bảng PlagiarismViolationRecord
        modelBuilder.Entity<PlagiarismViolationRecord>(entity =>
        {
            entity.ToTable("PlagiarismViolationRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.BannedKeyword).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CodeSnippet).IsRequired();
        });
    }
}

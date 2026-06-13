using CourseInquiryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseInquiryApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<CrmSyncLog> CrmSyncLogs => Set<CrmSyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(e =>
        {
            e.ToTable("Course");
            e.Property(c => c.CourseName).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Inquiry>(e =>
        {
            e.ToTable("CourseInquiries");
            e.Property(i => i.FirstName).IsRequired().HasMaxLength(100);
            e.Property(i => i.LastName).IsRequired().HasMaxLength(100);
            e.Property(i => i.Email).IsRequired().HasMaxLength(256);
            e.Property(i => i.Phone).HasMaxLength(50);
            e.Property(i => i.PreferredLocation).HasMaxLength(200);
            e.Property(i => i.Message).HasMaxLength(2000);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(i => i.Email);
            e.HasIndex(i => i.Status);
        });

        modelBuilder.Entity<CrmSyncLog>(e =>
        {
            e.ToTable("CrmSyncLogs");
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.ExternalId).HasMaxLength(100);
            e.Property(c => c.LastErrorCode).HasMaxLength(100);
            e.HasIndex(c => c.InquiryId);
        });
    }
}
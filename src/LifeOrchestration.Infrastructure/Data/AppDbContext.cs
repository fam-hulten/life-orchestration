using Microsoft.EntityFrameworkCore;
using LifeOrchestration.Core.Entities;

namespace LifeOrchestration.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Assignee).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.RecurrencePattern).HasConversion<int?>();
            entity.Property(e => e.ParentTaskId).IsRequired(false);
        });
    }
}

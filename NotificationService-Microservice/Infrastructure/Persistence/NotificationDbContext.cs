using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Common;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<NotificationDeliveryLog> NotificationDeliveryLogs { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }

            // Handle auditable entities
            if (entry.Entity is BaseAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Modified)
                {
                    auditableEntity.UpdatedBy = null; // Set from HttpContext if needed
                }
            }

            // Handle soft delete
            if (entry.Entity is ISoftDelete softDeleteEntity && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDeleteEntity.IsDeleted = true;
                softDeleteEntity.DeletedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore BaseEvent and all domain event classes
        modelBuilder.Ignore<BaseEvent>();
        
        var eventTypes = typeof(BaseEvent).Assembly.GetTypes()
            .Where(t => typeof(BaseEvent).IsAssignableFrom(t) && !t.IsAbstract);
        
        foreach (var eventType in eventTypes)
        {
            modelBuilder.Ignore(eventType);
        }

        // Apply entity configurations
        ConfigureNotification(modelBuilder);
        ConfigureNotificationTemplate(modelBuilder);
        ConfigureNotificationDeliveryLog(modelBuilder);

        // Global query filters for soft delete
        modelBuilder.Entity<Notification>().HasQueryFilter(n => !n.IsDeleted);
    }

    private void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.Source).IsRequired();
            entity.Property(e => e.IsRead).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.MetadataJson).HasColumnType("nvarchar(max)");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);

            entity.HasMany(e => e.DeliveryLogs)
                .WithOne(d => d.Notification)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }



    private void ConfigureNotificationTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TemplateKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.TitleTemplate).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ContentTemplate).HasMaxLength(5000).IsRequired();
            entity.Property(e => e.EmailSubjectTemplate).HasMaxLength(200);
            entity.Property(e => e.EmailBodyTemplate).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsActive).IsRequired();

            entity.HasIndex(e => e.TemplateKey).IsUnique();
            entity.HasIndex(e => e.Type);
        });
    }

    private void ConfigureNotificationDeliveryLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationDeliveryLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NotificationId).IsRequired();
            entity.Property(e => e.Channel).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.AttemptedAt).IsRequired();
            entity.Property(e => e.RetryCount).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            entity.HasIndex(e => e.NotificationId);
            entity.HasIndex(e => new { e.NotificationId, e.Channel });
            entity.HasIndex(e => e.Status);
        });
    }
}

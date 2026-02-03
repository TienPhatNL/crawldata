using Microsoft.EntityFrameworkCore;
using UserService.Domain.Common;
using UserService.Domain.Entities;
using UserService.Domain.Enums;

namespace UserService.Infrastructure.Persistence;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserApiKey> UserApiKeys { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<UserUsageRecord> UserUsageRecords { get; set; }
    public DbSet<UserPreference> UserPreferences { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<UserQuotaSnapshot> UserQuotaSnapshots { get; set; }
    public DbSet<AllowedEmailDomain> AllowedEmailDomains { get; set; }
    public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Domain.Entities.SubscriptionTier> SubscriptionTiers { get; set; }
    public DbSet<Announcement> Announcements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore BaseEvent and all domain event classes (they are not database entities)
        modelBuilder.Ignore<BaseEvent>();
        
        // Get all types that inherit from BaseEvent and ignore them
        var eventTypes = typeof(BaseEvent).Assembly.GetTypes()
            .Where(t => typeof(BaseEvent).IsAssignableFrom(t) && !t.IsAbstract);
        
        foreach (var eventType in eventTypes)
        {
            modelBuilder.Ignore(eventType);
        }

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Required properties
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();

            // Optional properties
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.InstitutionName).HasMaxLength(255);
            entity.Property(e => e.InstitutionEmail).HasMaxLength(255);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.StudentId).HasMaxLength(50);
            entity.Property(e => e.LecturerCredentials).HasMaxLength(255);
            entity.Property(e => e.StaffDepartment).HasMaxLength(100);
            entity.Property(e => e.AdminLevel).HasMaxLength(50);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(e => e.Biography).HasMaxLength(1000);
            entity.Property(e => e.TimeZone).HasMaxLength(50);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(10);
            entity.Property(e => e.TwoFactorSecret).HasMaxLength(100);
            entity.Property(e => e.EmailVerificationToken).HasMaxLength(255);
            entity.Property(e => e.PasswordResetToken).HasMaxLength(255);
            entity.Property(e => e.ApprovalNotes).HasMaxLength(1000);

            // Indexes
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CurrentSubscriptionPlanId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.LastLoginAt);
            entity.HasIndex(e => new { e.IsDeleted, e.Status });

            // Enum conversions
            entity.Property(e => e.Role).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();

            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Relationships
            entity.HasOne(e => e.CurrentSubscription)
                .WithOne(s => s.User)
                .HasForeignKey<UserSubscription>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ApiKeys)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UsageRecords)
                .WithOne(u => u.User)
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Preferences)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.QuotaSnapshot)
                .WithOne(q => q.User)
                .HasForeignKey<UserQuotaSnapshot>(q => q.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CurrentSubscriptionPlan)
                .WithMany()
                .HasForeignKey(e => e.CurrentSubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserApiKey configuration
        modelBuilder.Entity<UserApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.KeyHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.KeyPrefix).HasMaxLength(16).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IpWhitelist).HasMaxLength(1000);

            entity.Property(e => e.Scopes)
                .HasMaxLength(500);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.KeyPrefix);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.UserId, e.IsActive });

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // UserSubscription configuration
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.ExternalSubscriptionId).HasMaxLength(100);

            // Configure decimal precision for Price
            entity.Property(e => e.Price).HasPrecision(18, 2);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SubscriptionPlanId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.UserId, e.IsActive });

            entity.HasOne(e => e.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Global query filter to match User's soft delete filter
            entity.HasQueryFilter(e => !e.User.IsDeleted);
        });

        // UserUsageRecord configuration
        modelBuilder.Entity<UserUsageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResourceId).HasMaxLength(100);
            entity.Property(e => e.Metadata).HasColumnType("text");
            entity.Property(e => e.Currency).HasMaxLength(3);

            // Configure decimal precision for Cost
            entity.Property(e => e.Cost).HasPrecision(18, 2);

            entity.Property(e => e.UsageType).HasConversion<int>();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.UsageType);
            entity.HasIndex(e => e.UsageDate);
            entity.HasIndex(e => new { e.UserId, e.UsageDate });

            // Global query filter to match User's soft delete filter
            entity.HasQueryFilter(e => !e.User.IsDeleted);
        });

        // UserPreference configuration
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.Key }).IsUnique();

            // Global query filter to match User's soft delete filter
            entity.HasQueryFilter(e => !e.User.IsDeleted);
        });

        // UserSession configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionToken).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.RefreshToken).HasMaxLength(1000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.DeviceInfo).HasMaxLength(255);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.RefreshToken);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.IsActive });

            // Global query filter to match User's soft delete filter
            entity.HasQueryFilter(e => !e.User.IsDeleted);
        });

        // UserQuotaSnapshot configuration
        modelBuilder.Entity<UserQuotaSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).HasMaxLength(50);

            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.SubscriptionPlanId);
            entity.HasIndex(e => e.LastSynchronizedAt);

            entity.HasOne(e => e.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.User.IsDeleted);
        });

        // SubscriptionPayment configuration
        modelBuilder.Entity<SubscriptionPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
            entity.Property(e => e.PaymentLinkId).HasMaxLength(100);
            entity.Property(e => e.OrderCode).HasMaxLength(100);
            entity.Property(e => e.CheckoutUrl).HasMaxLength(1000);
            entity.Property(e => e.PaymentReference).HasMaxLength(200);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.Signature).HasMaxLength(255);
            entity.Property(e => e.PayOSPayload).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status).HasConversion<int>();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SubscriptionPlanId);
            entity.HasIndex(e => e.OrderCode).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AllowedEmailDomain configuration
        modelBuilder.Entity<AllowedEmailDomain>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Domain).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();

            // Unique index on Domain (case-insensitive)
            entity.HasIndex(e => e.Domain).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Content).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.Audience).HasConversion<int>();
            entity.Property(e => e.CreatedBy).IsRequired();
            entity.Property(e => e.PublishedAt).HasColumnType("datetime2");

            entity.HasIndex(e => e.Audience);
            entity.HasIndex(e => e.PublishedAt);
        });

        // SubscriptionPlan configuration
        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            
            // Store Features as JSON
            entity.Property(e => e.Features)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());

            // Foreign key to SubscriptionTier
            entity.HasOne(e => e.Tier)
                .WithMany(t => t.SubscriptionPlans)
                .HasForeignKey(e => e.SubscriptionTierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.SubscriptionTierId);
            entity.HasIndex(e => e.IsActive);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // SubscriptionTier configuration
        modelBuilder.Entity<Domain.Entities.SubscriptionTier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            
            entity.HasIndex(e => e.Level).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}
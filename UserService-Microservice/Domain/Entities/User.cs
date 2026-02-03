using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class User : BaseAuditableEntity, ISoftDelete
{
    public string Email { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public DateTime? EmailConfirmedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;

    // Subscription-related fields
    public Guid? CurrentSubscriptionId { get; set; }
    public Guid? CurrentSubscriptionPlanId { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public int CrawlQuotaUsed { get; set; } = 0;
    public int CrawlQuotaLimit { get; set; } = 4;
    public DateTime QuotaResetDate { get; set; }
    
    // Institution information (for Lecturers)
    public string? InstitutionName { get; set; }
    public string? InstitutionAddress { get; set; }
    public string? InstitutionEmail { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    
    // Staff approval fields
    public bool RequiresApproval { get; set; } = false;
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovalNotes { get; set; }
    
    // Role-specific properties
    public string? StudentId { get; set; } // For Students
    public string? LecturerCredentials { get; set; } // For Lecturers
    public string? StaffDepartment { get; set; } // For Staff
    public string? AdminLevel { get; set; } // For Admins
    
    // Profile information
    public string? ProfilePictureUrl { get; set; }
    public string? Biography { get; set; }
    public string? TimeZone { get; set; }
    public string? PreferredLanguage { get; set; } = "en";
    
    // Security settings
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    
    // Email verification
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpires { get; set; }

    // Password reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    
    // Suspension support
    public string? SuspensionReason { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public Guid? SuspendedById { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    public DateTime? ReactivatedAt { get; set; }
    public Guid? ReactivatedById { get; set; }
    
    // Soft delete support
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation properties
    public virtual UserSubscription? CurrentSubscription { get; set; }
    public virtual SubscriptionPlan? CurrentSubscriptionPlan { get; set; }
    public virtual ICollection<UserUsageRecord> UsageRecords { get; set; } = new List<UserUsageRecord>();
    public virtual ICollection<UserApiKey> ApiKeys { get; set; } = new List<UserApiKey>();
    public virtual ICollection<UserPreference> Preferences { get; set; } = new List<UserPreference>();
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public virtual UserQuotaSnapshot? QuotaSnapshot { get; set; }
    
    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public bool IsEmailConfirmed => EmailConfirmedAt.HasValue;
    public bool IsApproved => ApprovedAt.HasValue;
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    public bool CanLogin => Status == UserStatus.Active && !IsLocked && IsEmailConfirmed;
    
    // Returns the user's subscription tier entity (defaults to null if no subscription plan)
    public SubscriptionTier? SubscriptionTier => CurrentSubscriptionPlan?.Tier;
}
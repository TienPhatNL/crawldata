namespace UserService.Domain.Enums;

public enum UserStatus
{
    Pending = 0,        // Account created but not verified
    Active = 1,         // Fully active account
    Inactive = 2,       // Temporarily disabled
    Suspended = 3,      // Suspended due to policy violation
    Deleted = 4,        // Soft deleted
    PendingApproval = 5 // Waiting for staff approval (lecturers)
}
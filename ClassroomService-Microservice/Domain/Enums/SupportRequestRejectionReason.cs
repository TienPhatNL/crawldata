namespace ClassroomService.Domain.Enums;

public enum SupportRequestRejectionReason
{
    InsufficientPermissions = 1,
    RequireHigherAuth = 2,
    OutOfScope = 3,
    DuplicateRequest = 4,
    Other = 5
}

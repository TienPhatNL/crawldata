namespace WebCrawlerService.Application.Common.Security;

public static class Policies
{
    // Basic authentication
    public const string RequireAuthentication = "RequireAuthentication";

    // Role-based policies
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireStaffRole = "RequireStaffRole";
    public const string RequireLecturerRole = "RequireLecturerRole";
    public const string RequirePaidUser = "RequirePaidUser";

    // Resource access policies
    public const string CanAccessUserData = "CanAccessUserData";
    public const string CanManageCrawlJobs = "CanManageCrawlJobs";
    public const string CanAccessTemplates = "CanAccessTemplates";
    public const string CanManageCrawlers = "CanManageCrawlers";

    // Legacy/aliases
    public const string RequirePremiumTier = "RequirePremiumTier"; // Alias for RequirePaidUser
    public const string RequireEducatorRole = "RequireEducatorRole"; // Alias for RequireLecturerRole
}
namespace WebCrawlerService.Domain.Common;

public static class Policies
{
    public const string RequireAuthentication = "RequireAuthentication";
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireEducatorRole = "RequireEducatorRole";
    public const string RequirePremiumTier = "RequirePremiumTier";
    public const string CanAccessUserData = "CanAccessUserData";
    public const string CanManageCrawlers = "CanManageCrawlers";
}